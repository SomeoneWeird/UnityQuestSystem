using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

using Boxxen.Quests;

namespace Boxxen.Quests {
	public enum QuestType {
		none,
		destination,
		fetch,
		nested
	}

	public enum QuestStatus {
		notStarted,
		inProgress,
		completed,
		failed
	}

	public enum NestedQuestType {
		sequential,
		parallel
	}

    public class Quest {
        public delegate void OnStatusChangeHandler(QuestStatus newStatus);
		public event OnStatusChangeHandler OnStatusChange;

		public delegate void OnChildStatusChangeHandler (Quest childQuest, QuestStatus newStatus);
		public event OnChildStatusChangeHandler OnChildStatusChange;

		public delegate void OnCompleteHandler();
		public event OnCompleteHandler OnComplete;
		public delegate void OnFailureHandler();
		public event OnFailureHandler OnFailure;

		private string _name;
		private QuestStatus _status = QuestStatus.notStarted;

		private QuestType _type;

		private List<QuestItem> _rewards = new List<QuestItem>();

		private Quest _parentQuest;

		public string name { get { return _name; } }

		// **** Nested-quest variables
		private List<Quest> _childQuests = new List<Quest>();
		private NestedQuestType _childQuestType = NestedQuestType.sequential;
		// ****

		// **** Quest-type-specific variables here
		private Bounds _destination;

		private Timer _timer = null;
		private float _timeLimit = -1;
		private float _timeCurrent = -1;

		private List<QuestItem> _itemsToFetch = new List<QuestItem>();
		private List<QuestItem> _itemsFetched = new List<QuestItem>();
		// ****

		public Quest (string name, QuestType type) {
			_name = name;
			_type = type;
		}

		public QuestType GetQuestType () {
			return _type;
		}

		public string QuestTypeString () {
			switch(_type) {
				case QuestType.none: {
					return "None";
				}
				case QuestType.destination: {
					return "Destination";
				}
				case QuestType.nested: {
					return "Nested";
				}
				case QuestType.fetch: {
					return "Fetch";
				}
				default: {
					return "Unknown";
				}
			}
		}

		public QuestStatus GetStatus () {
			return _status;
		}

		public void SetStatus(QuestStatus status) {
			_status = status;
			if (OnStatusChange != null) {
				OnStatusChange(status);
			}
		}

		public void AddReward (QuestItem reward) {
			_rewards.Add(reward);
		}

		public List<QuestItem> GetRewards () {
			return _rewards;
		}

		public void SetDestination (Bounds destination) {
			_destination = destination;
		}

		public void SetTimeLimit (float seconds) {
			_timeLimit = seconds;
			_timeCurrent = seconds;
		}

		public bool HasTimeLimit () {
			return _timeLimit != -1;
		}

		public float GetTimeLimit () {
			return _timeLimit;
			
		}

		public void StopTimer () {
			_timer.Stop();
		}

		private void DestroyTimer () {
			if (_timer != null) {
				_timer.Start();
				return;
			}
		}

		public void ResetTimer () {
			_timeCurrent = _timeLimit;
		}

		private void timerSecondPassed (object source, ElapsedEventArgs e) {
			_timeCurrent -= 1;
			if (_timeCurrent <= 0) {
				timerExpired();
			}
		}

		private void timerExpired () {
			StopTimer();

			// Needs to run on the main thread
			UnityMainThreadDispatcher.Instance().Enqueue(() => {
				FailQuest();
			});
		}

		public double GetTimeRemaining () {
			return _timeCurrent;
		}

		public Bounds GetDestination () {
			return _destination;
		}

		public void CompleteQuest () {
			DestroyTimer();

			SetStatus(QuestStatus.completed);

			if (OnComplete != null) {
				OnComplete();
			}
		}

		public void FailQuest () {
			DestroyTimer();

			SetStatus(QuestStatus.failed);

			if (OnFailure != null) {
				OnFailure();
			}
		}

		private void SetParentQuest (Quest quest) {
			_parentQuest = quest;
		}

		public bool IsChildQuest () {
			return _parentQuest != null;
		}

		public void AddQuest(Quest quest) {
			quest.SetParentQuest(this);

			quest.OnStatusChange += (newStatus) => {
				if (OnChildStatusChange != null) {
					OnChildStatusChange(quest, newStatus);
				}
			};

			quest.OnChildStatusChange += (childQuest, newStatus) => {
				if(OnChildStatusChange != null) {
					OnChildStatusChange(childQuest, newStatus);
				}
			};

			quest.OnStatusChange += _ProcessChildQuests;

			_childQuests.Add(quest);
		}

		public List<Quest> GetChildQuests () {
			return _childQuests;
		}

		public void SetNestedQuestType (NestedQuestType type) {
			_childQuestType = type;
		}

		public void _ProcessChildQuests (QuestStatus newStatus) {
			// This function is called when a child quest has its status updated
			if (newStatus == QuestStatus.inProgress) {
				// Ignore newly in-progress quests
				return;
			}

			if (newStatus == QuestStatus.failed) {
				// Well, we failed, so fail all quests and the parent
				foreach(Quest quest in _childQuests) {
					quest.FailQuest();
				}
				
				FailQuest();

				return;
			}

			if (newStatus != QuestStatus.completed) {
				// Ignore anything else that isn't completed
				return;
			}

			if (_childQuests.TrueForAll(quest => quest.GetStatus() == QuestStatus.completed)) {
				// We finished all our quests! Mark this parent quest as completed.
				CompleteQuest();
				return;
			}

			if (_childQuestType == NestedQuestType.sequential) {
				for (int i = 0; i < _childQuests.Count; i++) {
					Quest quest = _childQuests[i];

					// Last
					if ((i + 1) == _childQuests.Count) {
						break;
					}

					Quest next = _childQuests[i + 1];

					if (quest.GetStatus() == QuestStatus.completed && next.GetStatus() == QuestStatus.notStarted) {
						next.Start();
					}
				}
			}
		}

		public void Start () {
			SetStatus(QuestStatus.inProgress);

			if (HasTimeLimit()) {
				DestroyTimer();
				_timer = new Timer(1000);
				_timer.Elapsed += this.timerSecondPassed;
				_timer.Start();
			}
			
			if (_type == QuestType.nested) {
				if (_childQuestType == NestedQuestType.sequential) {
					// Just start the first quest
					_childQuests[0].Start();
				} else if (_childQuestType == NestedQuestType.parallel) {
					foreach(Quest quest in _childQuests) {
						quest.Start();
					}
				}
			}
		}

		public void AddItemToFetch (QuestItem item) {
			_itemsToFetch.Add(item);
		}

		public void RemoveItemToFetch (QuestItem item) {
			_itemsToFetch.Remove(item);
		}

		public List<QuestItem> GetItemsToFetch () {
			return _itemsToFetch;
		}

		public void ItemFetched (QuestItem item) {
			List<QuestItem> hasFetched = _itemsFetched.FindAll(_item => _item.id == item.id);
			List<QuestItem> toFetch = _itemsToFetch.FindAll(_item => _item.id == item.id);

			if (toFetch.Count == 0 || hasFetched.Count < toFetch.Count) {
				hasFetched.Add(item);
			}

			if (toFetch.Count == hasFetched.Count) {
				CompleteQuest();
			}
		}

		public List<QuestItem> ItemsFetched () {
			return _itemsFetched;
		}
	}
}

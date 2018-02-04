using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

using Boxxen.Quests;
using Boxxen.Quests.Rewards;

namespace Boxxen.Quests {
	public enum QuestType {
		destination,
		fetch,
		multi
	}

	public enum QuestStatus {
		notStarted,
		inProgress,
		completed,
		failed
	}

	public enum MultiQuestType {
		sequential,
		parallel
	}

    public class Quest : QuestInterface {
        public delegate void OnStatusChangeHandler(QuestStatus newStatus);
		public event OnStatusChangeHandler OnStatusChange;

		public delegate void OnChildStatusChangeHandler (Quest childQuest, QuestStatus newStatus);
		public event OnChildStatusChangeHandler OnChildStatusChange;

		private string _name;
		private string _description;
		private QuestStatus _status = QuestStatus.notStarted;

		private QuestType _type;

		private List<QuestItem> _rewards = new List<QuestItem>();

		private Quest _parentQuest;

		public string name { get { return _name; } }
		public string description { get { return _description; } }

		// **** Multi-quest variables
		private List<Quest> _childQuests = new List<Quest>();
		private MultiQuestType _childQuestType = MultiQuestType.sequential;
		// ****

		// **** Quest-type-specific variables here
		private Bounds _destination;

		private Timer _timer = null;
		private float _timeLimit = -1;
		private float _timeCurrent = -1;

		private List<QuestItem> _itemsToFetch = new List<QuestItem>();
		private List<QuestItem> _itemsFetched = new List<QuestItem>();
		// ****

		public Quest (string name, string description, QuestType type) {
			_name = name;
			_description = description;
			_type = type;
		}

		public QuestType GetQuestType () {
			return _type;
		}

		public string QuestTypeString () {
			switch(_type) {
				case QuestType.destination: {
					return "Destination";
				}
				case QuestType.multi: {
					return "Multi";
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

		public void SetStatus(QuestStatus status, bool doNotNotifyParent = false) {
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

		public void StartTimer () {
			if (_timer != null) {
				_timer.Start();
				return;
			}
			_timer = new Timer(1000);
			_timer.Elapsed += this.timerSecondPassed;
			_timer.Start();
		}

		public void StopTimer () {
			_timer.Stop();
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
				SetStatus(QuestStatus.failed);
			});
		}

		public double GetTimeRemaining () {
			return _timeCurrent;
		}

		public Bounds GetDestination () {
			return _destination;
		}

		public void CompleteQuest () {
			if (_timer != null) {
				StopTimer();
				_timer = null;
			}

			SetStatus(QuestStatus.completed);
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

		public void SetMultiQuestType (MultiQuestType type) {
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
					quest.SetStatus(QuestStatus.failed, true);
				}
				
				// Do not pass true, so we notify our parent
				// if we're also a child quest.
				SetStatus(QuestStatus.failed);

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

			if (_childQuestType == MultiQuestType.sequential) {
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
			
			if (_type == QuestType.multi) {
				if (_childQuestType == MultiQuestType.sequential) {
					// Just start the first quest
					_childQuests[0].Start();
				} else if (_childQuestType == MultiQuestType.parallel) {
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

		// Returns true if item causes quest to be completed
		public bool ItemFetched (QuestItem item) {
			List<QuestItem> hasFetched = _itemsFetched.FindAll(_item => _item.name == item.name);
			List<QuestItem> toFetch = _itemsToFetch.FindAll(_item => _item.name == item.name);

			if (toFetch.Count == 0 || hasFetched.Count < toFetch.Count) {
				hasFetched.Add(item);
			}

			if (toFetch.Count == hasFetched.Count) {
				CompleteQuest();
				return true;
			} else {
				return false;
			}
		}

		public List<QuestItem> ItemsFetched () {
			return _itemsFetched;
		}
	}
}

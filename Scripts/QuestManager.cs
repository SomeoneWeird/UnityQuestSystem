using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Boxxen.Quests;
using Boxxen.Quests.Rewards;

public class QuestManager : MonoBehaviour {
    public delegate void OnNewQuestHandler(Quest quest);
	public event OnNewQuestHandler OnNewQuest;
	public delegate void OnQuestStatusChangeHandler(Quest quest, QuestStatus newStatus);
	public event OnQuestStatusChangeHandler OnQuestStatusChange;

    private List<Quest> quests = new List<Quest>();

	void Start () {
		// We need to create a new instance of UnityMainThreadDispatcher
		GameObject unityMainThreadDispatcherObject = GameObject.Find("__UnityMainThreadDispatcher");
		if (unityMainThreadDispatcherObject == null) {
			unityMainThreadDispatcherObject = new GameObject("__UnityMainThreadDispatcher");
			unityMainThreadDispatcherObject.AddComponent<UnityMainThreadDispatcher>();
		}
	}

	public void AddQuest (Quest quest) {
		quests.Add(quest);
		if (OnNewQuest != null) {
			OnNewQuest(quest);
		}

		quest.OnStatusChange += (newStatus) => _OnQuestStatusChange(quest, newStatus);
	}

	private void _OnQuestStatusChange (Quest quest, QuestStatus newStatus) {
		if (OnQuestStatusChange != null) {
			OnQuestStatusChange(quest, newStatus);
		}
	}

	public List<Quest> GetQuests () {
		return quests;
	}

	public List<Quest> GetQuests (QuestStatus filter) {
		return quests.FindAll(q => q.GetStatus() == filter);
	}

	public void StartTrackingLocation (float interval = 1f) {
		InvokeRepeating("CheckLocation", 0f, interval);
	}

	public void StopTrackingLocation () {
		CancelInvoke("CheckLocation");
	}

	public void CheckLocation () {
		Bounds bounds = GetComponent<Renderer>().bounds;

		Renderer[] renderers = GetComponentsInChildren<Renderer>();

		foreach(Renderer renderer in renderers) {
			bounds.Encapsulate(renderer.bounds);
		}

		List<Quest> quests = GetQuests(QuestStatus.inProgress);

		foreach(Quest quest in quests) {
			CheckQuestLocation(quest, bounds);
		}
	}

	private void CheckQuestLocation (Quest quest, Bounds bounds) {
		if (quest.GetQuestType() == QuestType.multi) {
			List<Quest> childQuests = quest.GetChildQuests();
			foreach(Quest childQuest in childQuests) {
				if (childQuest.GetStatus() == QuestStatus.inProgress) {
					CheckQuestLocation(childQuest, bounds);
				}
			}
			return;
		}

		bool touching = quest.GetDestination().Intersects(bounds);
		if (touching) {
			quest.CompleteQuest();
		}
	}

	public void ItemFetched (QuestItem item) {
		foreach (Quest quest in quests) {
			quest.ItemFetched(item);
		}
	}
}

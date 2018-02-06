using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Boxxen.Quests;

public class QuestManager : MonoBehaviour {
    public delegate void OnNewQuestHandler(Quest quest);
	public event OnNewQuestHandler OnNewQuest;

	public delegate void OnQuestStatusChangeHandler(Quest quest, QuestStatus newStatus);
	public event OnQuestStatusChangeHandler OnQuestStatusChange;

	public delegate void OnQuestCompletionHandler(Quest quest);
	public event OnQuestCompletionHandler OnQuestCompletion;

	public delegate void OnQuestFailureHandler(Quest quest);
	public event OnQuestFailureHandler OnQuestFailure;

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
		quest.OnComplete += () => _OnQuestCompletion(quest);
		quest.OnFailure += () => _OnQuestFailure(quest);
	}

	private void _OnQuestStatusChange (Quest quest, QuestStatus newStatus) {
		if (OnQuestStatusChange != null) {
			OnQuestStatusChange(quest, newStatus);
		}
	}

	private void _OnQuestCompletion(Quest quest) {
		if (OnQuestCompletion != null) {
			OnQuestCompletion(quest);
		}
	}

	private void _OnQuestFailure(Quest quest) {
		if (OnQuestFailure != null) {
			OnQuestFailure(quest);
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
		if (quest.GetQuestType() == QuestType.nested) {
			List<Quest> childQuests = quest.GetChildQuests();
			foreach(Quest childQuest in childQuests) {
				if (childQuest.GetStatus() == QuestStatus.inProgress) {
					CheckQuestLocation(childQuest, bounds);
				}
			}
			return;
		}

		if (quest.GetDestination().Intersects(bounds)) {
			quest.CompleteQuest();
		}
	}

	public void ItemFetched (QuestItem item) {
		foreach (Quest quest in quests) {
			quest.ItemFetched(item);
		}
	}
}

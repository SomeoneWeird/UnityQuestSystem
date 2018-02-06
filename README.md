# UnityQuestSystem

A super simplistic, but powerful quest system for Unity games. 

Quests can be entirely managed by you or you can use one of the build in preset types:
    - Destination based
    - Item fetching (works with any inventory system!)

Quests can optional be timed, and automatically fail if they time out.

Quests can also be nested to unlimited depth, meaning your quests can have children, who have children, who have children...

You can require these nested quests to either be completed in parallel or sequentially.

Quests can also have optional rewards that you should give to the player once they have completed it.

## Example

Give a quest to a player that contains a `QuestManager`

```cs
void OnTriggerEnter (Collider other) {
    // Create a new destination-based quest
    Quest quest = new Quest("Get to the destination!", QuestType.destination);

    // Here we find our destination, and pass it's coordinates to the quest 
    quest.SetDestination(GameObject.Find("Destination").GetComponent<BoxCollider>().bounds);

    // Let's add a time limit to this quest, this is completely optional though.
    // 1 minute should be enough time.
    quest.SetTimeLimit(60);

    // Lets start our quest. This will set its status to inProgress
    // and also start the time limit if you've set one.
    quest.Start();

    // Here we add it to our quest manager, so it can help track all of our quests.
    other.GetComponent<QuestManager>().AddQuest(quest);
}
```

## QuestReward

A basic interface describing a reward.
Your inventory items should fulfil this interface so you can cast them.

```
id: string
```

Example class:

```cs
using Boxxen.Quests;

class InventoryItem : QuestItem {
    private string _id;
    public InventoryItem (string id) {
        _id = id;
    }
    public string id { get { return _id; } }
}
```

## QuestType

An enum of different quest types.

Possible values:
    - **none**: Manage the quest entirely on your own.
    - **destination**: Make the player go to a specific location
    - **fetch**: Make the player fetch specific items
    - **multi**: A "parent" quest which will have nested quests

## QuestStatus

An enum of different quest statuses.

Possible values:
    - **notStarts**: The quest has not started.
    - **inProgress**: The quest is in progress.
    - **completed**: The quest has been completed.
    - **failed**: The quest has been failed.

## MultiQuestType

An enum describing how nested quests should be treated.

Possible values:
    - **sequential**: Require quests be completed in the order they were added.
    - **parallel**: Allow the quests to be completed it any order.

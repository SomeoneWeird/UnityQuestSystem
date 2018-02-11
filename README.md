# UnityQuestSystem

A super simplistic, but powerful quest system for Unity games. 

Quests can be entirely managed by you or you can use one of the build in preset types:

- Destination based

- Item fetching (works with any inventory system!)

Quests can optional be timed, and automatically fail if they time out.

Quests can also be nested to unlimited depth, meaning your quests can have children, who have children, who have children...

You can require these nested quests to either be completed in parallel or sequentially.

Quests can also have optional rewards that you should give to the player once they have completed it.

# Example

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

# API

## **QuestReward**

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

## **QuestType**

An enum of different quest types.

Possible values:
- **none**: Manage the quest entirely on your own.
- **destination**: Make the player go to a specific location
- **fetch**: Make the player fetch specific items
- **nested**: A "parent" quest which will have nested quests

## **QuestStatus**

An enum of different quest statuses.

Possible values:
- **notStarts**: The quest has not started.
- **inProgress**: The quest is in progress.
- **completed**: The quest has been completed.
- **failed**: The quest has been failed.

## **NestedQuestType**

An enum describing how nested quests should be treated.

Possible values:
- **sequential**: Require quests be completed in the order they were added.
- **parallel**: Allow the quests to be completed it any order.

## **QuestManager()**

A _QuestManager_ maintains a list of quests, multiple _QuestManagers_ can exist can exist in a single scene, allowing you to manage different character quest lists for example.

Using a _QuestManager_ is not required, you can manage your _Quest_s directly if you wish.

### **QuestManager#AddQuest(Quest quest)**

Add a new quest to this instance of _QuestManager_

### **QuestManager#GetQuests()**

Returns `List<Quest>`

Returns a list of quests that are managed by this _QuestManager_

### **QuestManager#GetQuests(QuestStatus filter)**

Returns `List<Quest>`

Returns a list of quests that are managed by this _QuestManager_, filtering out those which do not match the filter passed in.

### **QuestManager#StartTrackingLocation(float interval = 1f)**

Every _interval_ **QuestManager#CheckLocation()** will be called.

This is a helper function as is _not_ required to be used if you want to manage this by yourself using triggers or another mechanism.

### **QuestManager#StopTrackingLocation()**

If **QuestManager#StartTrackingLocation** has been called, calling this function will stop tracking the position of the _GameObject_ this _GameManager_ is attached to.

### **QuestManager#CheckLocation()**

The _GameObject_ that this instance of _QuestManager_ is attached to will be checked against the current _QuestStatus.inProgress_ quests. If the location overlaps with any of the quests destinations, that quest will be marked as complete.

This is a helper function as is _not_ required to be used if you want to manage this by yourself using triggers or another mechanism.

### **QuestManager#ItemFetched(QuestItem item)**

This function will iterate over all quests and call **Quest#ItemFetched(item)**

This is a helper function as is _not_ required to be used if you want to manage this by yourself using triggers or another mechanism.

## **Quest(string name, QuestType type)**

Initialise a new quest.

### **Quest#GetQuestType()**

Returns `QuestType`

### **Quest#QuestTypeString()**

Returns `string`

The string equivalent of the current quest type

### **Quest#GetStatus()**

Returns `QuestStatus`

### **Quest#SetStatus(QuestStatus newStatus)**

Sets the status of the quest.

### **Quest#AddReward(QuestItem reward)**

Add a new reward that should be granted upon quest completion.

### **Quest#GetRewards()**

Returns `List<QuestItem>`

### **Quest#SetDestination(Bounds destination)**

Only to be used when the quest type is set to `QuestType.destination`.

Sets the destination of the quest.

### **Quest#GetDestination()**

Returns `Bounds`

Returns the destination of a quest if one has been set.

**Example:**

The easiest way to get a `Bounds` instance is to get it from a `Collider` on your destination object.

```
quest.SetDestination(GameObject.Find("Destination").GetComponent<BoxCollider>().bounds);
```

### **Quest#SetTimeLimit(float seconds)**

Give this quest a time limit.

Time will only count down once `Quest#Start()` has been called.

If the time limit gets to 0 before the quest has been completed, it will be failed.

### **Quest#HasTimeLimit()**

Returns `bool`

Returns `true` if the quest has a time limit.

### **Quest#GetTimeLimit()**

Returns `float`

Returns the total time limit if one is set. This does **not** return the remaining time if a countdown has already started.

### **Quest#GetTimeRemaining()**

Returns `float`

Returns the remaining time of a countdown if one has started.

### **Quest#ResetTimer()**

Resets the countdown timer if one is set.

### **Quest#CompleteQuest()**

This will complete a quest. 

`Quest#GetStatus()` will return `QuestStatus.completed`.

Quest completion events will also fire.

### **Quest#FailQuest()**

This will fail a quest.

`Quest#GetStatus()` will return `QuestStatus.failed`.

Quest failure events will also fire.

### **Quest#IsChildQuest()**

Returns `bool`. 

Returns `true` if this quest is a child of a nested quest.

### **Quest#AddQuest(Quest childQuest)**

Only to be used when quest type is set to `QuestType.nested`.

Adds a child quest to this quest.

### **Quest#GetChildQuests()**

Returns `List<Quest>`.

Returns a list of child quests.

### **Quest#SetNestedQuestType(NestedQuestType type)**

Will change the required completion order of child quests.

If `NestedQuestType.sequential` is passed, child quests must be completed in the order they were added to this quest.

If `NestedQuestType.parallel` is passed, child quests can be completed in any order.

### **Quest#AddItemToFetch(QuestItem item)**

Only to be used when the quest type is set to `QuestType.fetch`.

Adds an item that is required to be fetched.

### **Quest#RemoveItemToFetch(QuestItem item)**

Only to be used when the quest type is set to `QuestType.fetch`.

Removes an item that is required to be fetched.

### **Quest#GetItemsToFetch()**

Returns `List<QuestItem>`

### **Quest#GetItemsFetched()**

Returns `List<QuestItem>`

### **Quest#ItemFetched(QuestItem item)**

To be called when the player has received an item.

### **Quest#Start()**

Starts a quest.

If the quest has a time limit, it will be started.

### **Quest~OnStatusChange(QuestStatus newStatus)**

An event that is emitted when the quests status has changed.

Example:

```cs
myQuest.OnStatusChange += (status) => {
    Debug.Log("myQuest new status:" + status);
}
```

### **Quest~OnChildStatusChange(Quest childQuest, QuestStatus newStatus)**

An event that is emitted when a nested child quest status changes.

### **Quest~OnComplete()**

This event is emitted when the quest has been completed.

### **Quest~OnFailure()**

This event is emitted when the quest has been failed.

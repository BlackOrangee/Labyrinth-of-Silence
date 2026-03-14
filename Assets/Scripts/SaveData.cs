using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class SaveData
    {
        public string saveName = "Save";
        public string saveDateTime;
        public string screenshotPath;
        public string chapter = "Chapter 1";
        public string currentQuest = "";

        public PlayerData playerData;

        public InventoryData inventoryData;

        public List<QuestData> questsData;

        public string sceneName;
        public int sceneBuildIndex = -1;

        public List<GameObjectState> gameObjectStates;

        public SaveData()
        {
            saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            playerData = new PlayerData();
            inventoryData = new InventoryData();
            questsData = new List<QuestData>();
            gameObjectStates = new List<GameObjectState>();
        }
    }

    [Serializable]
    public class GameObjectState
    {
        public string uniqueId;
        public string objectName;
        public bool isActive;
        public TransformData transformData;

        public DoorState doorState;
        public EnemyState enemyState;
        public CollectItemState collectItemState;
        public LanternState lanternState;
        public LampState lampState;
        public ActivatableState activatableState;
        public GeneratorTaskState generatorTaskState;
        public KeypadState keypadState;
        public FuelCanisterState fuelCanisterState;
        public NewspaperPickupState newspaperPickupState;
        public ThoughtTriggerState thoughtTriggerState;
    }

    [Serializable]
    public class TransformData
    {
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
        public float scaleX, scaleY, scaleZ;

        public TransformData() { }

        public TransformData(Transform transform)
        {
            posX = transform.position.x;
            posY = transform.position.y;
            posZ = transform.position.z;

            rotX = transform.rotation.x;
            rotY = transform.rotation.y;
            rotZ = transform.rotation.z;
            rotW = transform.rotation.w;

            scaleX = transform.localScale.x;
            scaleY = transform.localScale.y;
            scaleZ = transform.localScale.z;
        }

        public Vector3 GetPosition() => new Vector3(posX, posY, posZ);
        public Quaternion GetRotation() => new Quaternion(rotX, rotY, rotZ, rotW);
        public Vector3 GetScale() => new Vector3(scaleX, scaleY, scaleZ);
    }

    [Serializable]
    public class DoorState
    {
        public bool isOpen;
        public bool isLocked;
        public float pivotRotX, pivotRotY, pivotRotZ;
        public float pivotRotW = 1f;
        public bool hasPivotB;
        public float pivotBRotX, pivotBRotY, pivotBRotZ;
        public float pivotBRotW = 1f;
    }

    [Serializable]
    public class EnemyState
    {
        public string currentState;
        public int currentPatrolIndex;
        public float waitTimer;
        public Vector3Data lastKnownPlayerPosition;
    }

    [Serializable]
    public class CollectItemState
    {
        public bool isCollected;
    }

    [Serializable]
    public class LanternState
    {
        public float currentRange;
        public float currentIntensity;
        public int currentModeIndex;
    }

    [Serializable]
    public class LampState
    {
        public float currentFuel;
        public int lightMode;
    }

    [Serializable]
    public class ActivatableState
    {
        public bool isActivated;
    }

    [Serializable]
    public class GeneratorTaskState
    {
        public bool isRepaired;
        public bool hasFuel;
    }

    [Serializable]
    public class KeypadState
    {
        public bool isUnlocked;
    }

    [Serializable]
    public class FuelCanisterState
    {
        public float canisterCapacity;
    }

    [Serializable]
    public class NewspaperPickupState
    {
        public bool isPickedUp;
    }

    [Serializable]
    public class ThoughtTriggerState
    {
        public bool hasBeenTriggered;
    }

    [Serializable]
    public class Vector3Data
    {
        public float x, y, z;

        public Vector3Data() { }

        public Vector3Data(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public class PlayerData
    {
        public float posX;
        public float posY;
        public float posZ;

        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW;

        public float currentSanity = -1f;

        public PlayerData()
        {
        }

        public PlayerData(Transform playerTransform)
        {
            posX = playerTransform.position.x;
            posY = playerTransform.position.y;
            posZ = playerTransform.position.z;

            rotX = playerTransform.rotation.x;
            rotY = playerTransform.rotation.y;
            rotZ = playerTransform.rotation.z;
            rotW = playerTransform.rotation.w;
        }

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        public Quaternion GetRotation()
        {
            return new Quaternion(rotX, rotY, rotZ, rotW);
        }
    }

    [Serializable]
    public class InventoryData
    {
        public List<string> items;
        public List<KeyColorType> collectedKeys;
        public List<string> collectedNewspaperIds;

        public InventoryData()
        {
            items = new List<string>();
            collectedKeys = new List<KeyColorType>();
            collectedNewspaperIds = new List<string>();
        }
    }

    [Serializable]
    public class PlayerStatsTransitData
    {
        public float sanity = -1f;
        public float fuel = -1f;
        public int lampMode = -1;
    }

    [Serializable]
    public class QuestData
    {
        public string questId;
        public string description;
        public int currentProgress;
        public int targetProgress;
        public bool isCompleted;

        public QuestData()
        {
        }

        // public QuestData(Quest quest)
        // {
        //     questId = quest.questId;
        //     description = quest.description;
        //     currentProgress = quest.currentProgress;
        //     targetProgress = quest.targetProgress;
        //     isCompleted = quest.isCompleted;
        // }
    }
}

﻿using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * 	<objective type="GotoPOISDX, Mods" value="500-800" phase="1">
			<property name="completion_distance" value="50" />
            <property name="PrefabName" value="prefabName" />
		</objective>
*/
class ObjectiveGotoPOISDX : ObjectiveRandomPOIGoto
{
    String strPOIname = "";
    
    public override BaseObjective Clone()
    {
        ObjectiveGotoPOISDX objectiveBuff = new ObjectiveGotoPOISDX();
        this.CopyValues(objectiveBuff);
        return objectiveBuff;
    }



    private void SetDistanceOffset(Vector3 POISize)
    {
        if (POISize.x > POISize.z)
        {
            this.distanceOffset = POISize.x;
        }
        else
        {
            this.distanceOffset = POISize.z;
        }
    }

    
    protected override Vector3 GetPosition(EntityNPC ownerNPC, List<Vector2> usedPOILocations, int entityIDforQuests )
    {
        if (base.OwnerQuest.GetPositionData(out this.position, Quest.PositionDataTypes.POIPosition))
        {
            base.OwnerQuest.Position = this.position;
            Vector3 distanceOffset;
            base.OwnerQuest.GetPositionData(out distanceOffset, Quest.PositionDataTypes.POISize);
            this.SetDistanceOffset(distanceOffset);
            this.positionSet = true;
            base.OwnerQuest.HandleMapObject(Quest.PositionDataTypes.POIPosition, this.icon);
            base.CurrentValue = 2;
            return this.position;
        }
        EntityAlive entityAlive = ownerNPC;

        if (ownerNPC == null)
            entityAlive = base.OwnerQuest.OwnerJournal.OwnerPlayer;
        if (Steam.Network.IsServer)
        {
            PrefabInstance randomPOINearWorldPos = GetRandomPOINearWorldPos(new Vector2(entityAlive.position.x, entityAlive.position.z), 1000, 50000000, base.OwnerQuest.QuestTags, base.OwnerQuest.QuestClass.DifficultyTier, usedPOILocations, entityIDforQuests);
            if (randomPOINearWorldPos != null)
            {
                Vector2 vector = new Vector2((float)randomPOINearWorldPos.boundingBoxPosition.x + (float)randomPOINearWorldPos.boundingBoxSize.x / 2f, (float)randomPOINearWorldPos.boundingBoxPosition.z + (float)randomPOINearWorldPos.boundingBoxSize.z / 2f);
                if (vector.x == -0.1f && vector.y == -0.1f)
                {
                    return Vector3.zero;
                }
                int num = (int)vector.x;
                int num2 = (int)entityAlive.position.y;
                int num3 = (int)vector.y;
                this.position = new Vector3((float)num, (float)num2, (float)num3);
                if (GameManager.Instance.World.IsPositionInBounds(this.position))
                {
                    base.OwnerQuest.Position = this.position;
                    base.FinalizePoint(new Vector3((float)randomPOINearWorldPos.boundingBoxPosition.x, (float)randomPOINearWorldPos.boundingBoxPosition.y, (float)randomPOINearWorldPos.boundingBoxPosition.z), new Vector3((float)randomPOINearWorldPos.boundingBoxSize.x, (float)randomPOINearWorldPos.boundingBoxSize.y, (float)randomPOINearWorldPos.boundingBoxSize.z));
                    base.OwnerQuest.QuestPrefab = randomPOINearWorldPos;
                    base.OwnerQuest.DataVariables.Add("POIName", base.OwnerQuest.QuestPrefab.filename);
                    if (usedPOILocations != null)
                    {
                        usedPOILocations.Add(new Vector2((float)randomPOINearWorldPos.boundingBoxPosition.x, (float)randomPOINearWorldPos.boundingBoxPosition.z));
                    }
                    base.OwnerQuest.HandleMapObject(Quest.PositionDataTypes.POIPosition, this.icon);
                    return this.position;
                }
            }
        }
        else
        {
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(new NetPackageQuestGotoPoint(entityAlive.entityId, base.OwnerQuest.QuestTags, base.OwnerQuest.QuestCode, NetPackageQuestGotoPoint.QuestGotoTypes.RandomPOI, base.OwnerQuest.QuestClass.DifficultyTier, 0, -1, 0f, 0f, 0f, -1f), false);
            base.CurrentValue = 1;
        }
        return Vector3.zero;
    }

    public virtual PrefabInstance GetRandomPOINearWorldPos(Vector2 worldPos, int minSearchDistance, int maxSearchDistance, QuestTags questTag, byte difficulty, List<Vector2> usedPOILocations, int entityIDforQuests)
    {
        List<PrefabInstance> poiPrefabs = GameManager.Instance.World.ChunkClusters[0].ChunkProvider.GetDynamicPrefabDecorator().GetPOIPrefabs();
        Vector2 vector = new Vector2(-0.1f, -0.1f);
        for (int i = 0; i < 1000; i++)
        {
            int index = UnityEngine.Random.Range(0, poiPrefabs.Count);
            PrefabInstance prefabInstance = poiPrefabs[index];
            if (prefabInstance.prefab.bSleeperVolumes && prefabInstance.prefab.PrefabName == strPOIname)
            {
                Vector2 vector2 = new Vector2((float)prefabInstance.boundingBoxPosition.x, (float)prefabInstance.boundingBoxPosition.z);
                if (usedPOILocations == null || !usedPOILocations.Contains(vector2))
                {
                    QuestEventManager.POILockoutReasonTypes poilockoutReasonTypes = QuestEventManager.Current.CheckForPOILockouts(entityIDforQuests, vector2);
                    if (poilockoutReasonTypes != QuestEventManager.POILockoutReasonTypes.None)
                    {
                        Debug.Log("Quest POI Locked Out: " + prefabInstance.filename + " for " + poilockoutReasonTypes.ToStringCached<QuestEventManager.POILockoutReasonTypes>());
                    }
                    else
                    {
                        Vector2 b = new Vector2((float)prefabInstance.boundingBoxPosition.x + (float)prefabInstance.boundingBoxSize.x / 2f, (float)prefabInstance.boundingBoxPosition.z + (float)prefabInstance.boundingBoxSize.z / 2f);
                        float sqrMagnitude = (worldPos - b).sqrMagnitude;
                        if (sqrMagnitude < (float)maxSearchDistance && sqrMagnitude > (float)minSearchDistance)
                        {
                            return prefabInstance;
                        }
                    }
                }
            }
        }
        return null;
    }

public override void ParseProperties(DynamicProperties properties)
{
    base.ParseProperties(properties);

    if (properties.Values.ContainsKey("PrefabName"))
        this.strPOIname = properties.Values["PrefabName"];
}

protected override bool useUpdateLoop
{
    get
    {
        return true;
    }
}
}

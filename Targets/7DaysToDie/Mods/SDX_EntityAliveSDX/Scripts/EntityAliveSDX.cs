﻿/*
 * Class: EntityAliveSDX
 * Author:  sphereii 
 * Category: Entity
 * Description:
 *      This mod is an extension of the base entityAlive. This is meant to be a base class, where other classes can extend
 *      from, giving them the ability to accept quests and buffs.
 * 
 * Usage:
 *      Add the following class to entities that are meant to use these features. 
 *
 *      <property name="Class" value="EntityAliveSDX, Mods" />
 */
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

class EntityAliveSDX : EntityAlive
{
    public QuestJournal QuestJournal = new QuestJournal();
    public List<String> lstQuests = new List<String>();
    public Orders currentOrder = Orders.Wander;
    public List<Vector3> PatrolCoordinates = new List<Vector3>();
    public int HireCost = 1000;
    public ItemValue HireCurrency = ItemClass.GetItem("casinoCoin", false);

    String strMyName = "Bob";
    public System.Random random = new System.Random();

    private bool blDisplayLog = true;
    public void DisplayLog(String strMessage)
    {
        if (blDisplayLog &&  !this.IsDead())
            Debug.Log(this.entityName + ": " + strMessage);
    }

    // These are the orders, used in cvars for the EAI Tasks. They are casted as floats.
    public enum Orders
    {
        Follow = 0,
        Stay = 1,
        Wander = 2,
        None = 3,
        SetPatrolPoint = 4,
        EndPatrolPoint = 5,
        Patrol = 6, 
        Hire = 7
    }

    // Over-ride for CopyProperties to allow it to read in StartingQuests.
    public override void CopyPropertiesFromEntityClass()
    {
        base.CopyPropertiesFromEntityClass();
        EntityClass entityClass = EntityClass.list[this.entityClass];

        // Read in a list of names then pick one at random.
        if (entityClass.Properties.Values.ContainsKey("Names"))
        {
            string text = entityClass.Properties.Values["Names"];
            string[] Names = text.Split(',');
            int index = random.Next(0, Names.Length);
            strMyName = Names[index];
        }

        if (entityClass.Properties.Values.ContainsKey("HireCost"))
            HireCost = int.Parse( entityClass.Properties.Values["HireCost"]);
        if (entityClass.Properties.Values.ContainsKey("HireCurrency"))
        {
            this.HireCurrency = ItemClass.GetItem(entityClass.Properties.Values["HireCurrency"], false);
            if (this.HireCurrency.IsEmpty())
                this.HireCurrency = ItemClass.GetItem("casinoCoin", false);
        }


    }

    public override Ray GetLookRay()
    {
        Ray result = new Ray(this.position + new Vector3(0f, this.GetEyeHeight() * 1f, 0f), this.GetLookVector());
        return result;
    }
    public override Vector3 GetLookVector()
    {
        if (this.lookAtPosition.Equals(Vector3.zero))
        {
            return base.GetLookVector();
        }
        return Vector3.Normalize(this.lookAtPosition - this.getHeadPosition());
    }
    public override float GetSeeDistance()
    {
        return 80f;
    }

    public override EntityActivationCommand[] GetActivationCommands(Vector3i _tePos, EntityAlive _entityFocusing)
    {
        EntityActivationCommand[] ActivationCommands = new EntityActivationCommand[]
        {
            new EntityActivationCommand("TellMe", "talk", true ),
            new EntityActivationCommand("FollowMe", "talk", isTame( _entityFocusing)),
            new EntityActivationCommand("StayHere", "talk", isTame( _entityFocusing )),
            new EntityActivationCommand("HangOut", "talk", isTame( _entityFocusing )),
            new EntityActivationCommand("SetPatrol", "talk", isTame( _entityFocusing)),
            new EntityActivationCommand("Patrol", "talk", isTame( _entityFocusing)),
            new EntityActivationCommand("Hire", "talk", true)
        };

        return ActivationCommands;
    }

    public override bool OnEntityActivated(int _indexInBlockActivationCommands, Vector3i _tePos, EntityAlive _entityFocusing)
    {
        this.emodel.avatarController.SetBool("IsBusy", true);

        switch (_indexInBlockActivationCommands)
        {
            case 0: // Tell me about yourself
                GameManager.ShowTooltipWithAlert(_entityFocusing as EntityPlayerLocal, this.ToString() + "\n\n\n\n\n", "ui_denied");
                break;
            case 1: // Follow me
                this.Buffs.SetCustomVar("Leader", _entityFocusing.entityId, true);
                this.Buffs.SetCustomVar("CurrentOrder", (float)Orders.Follow, true);
                break;
            case 2: // Stay Here
                this.Buffs.SetCustomVar("CurrentOrder", (float)Orders.Stay, true);
                break;
            case 3: // Hang out / wander here
                this.Buffs.SetCustomVar("CurrentOrder", (float)Orders.Wander, true);
                break;
            case 4: // Set Patrol Point
                this.Buffs.SetCustomVar("Leader", _entityFocusing.entityId, true);
                this.Buffs.SetCustomVar("CurrentOrder", (float)Orders.SetPatrolPoint, true);
                this.PatrolCoordinates.Clear(); // Clear the existing point.
                break;
            case 5: // end patrol Point
                this.Buffs.SetCustomVar("Leader", 0, true);
                this.Buffs.SetCustomVar("CurrentOrder", (float)Orders.Patrol, true);
                break;
            case 6:
                if ( _entityFocusing is EntityPlayerLocal)
                    Hire(_entityFocusing as EntityPlayerLocal);
                break;
            default:
                break;
        }

        return true;
    }

    public virtual bool isTame(EntityAlive _player )
    {
        if (this.Buffs.HasCustomVar("Leader") && this.Buffs.GetCustomVar("Leader") == (float)_player.entityId)
            return true;
        if (this.Buffs.HasCustomVar("Owner") && this.Buffs.GetCustomVar("Owner") == (float)_player.entityId)
            return true;

        return false;
    }


    public virtual void Hire(EntityPlayerLocal _player)
    {
        LocalPlayerUI uiforPlayer = LocalPlayerUI.GetUIForPlayer(_player as EntityPlayerLocal);
        if (null != uiforPlayer)
        {
            if (uiforPlayer.xui.PlayerInventory.GetItemCount(this.HireCurrency) > this.HireCost)
            {
                // Create the stack of currency
                ItemStack stack = new ItemStack(this.HireCurrency, this.HireCost);
                uiforPlayer.xui.PlayerInventory.RemoveItems(new ItemStack[] { stack }, 1);

                // Add the stack of currency to the NPC, and set its orders.
                this.inventory.AddItem(stack);
                this.Buffs.SetCustomVar("Owner", _player.entityId, true);
                this.Buffs.SetCustomVar("CurrentOrder", (float)Orders.Follow, true);
            }
            else
            {
                GameManager.ShowTooltipWithAlert(_player, "You cannot afford me. I want " + this.HireCost + " " + this.HireCurrency, "ui_denied");
            }
        }
    }
    public override void PostInit()
    {
        base.PostInit();
        InvokeRepeating("DisplayStats", 0f, 60f);
    }

    
    public virtual void UpdatePatrolPoints( Vector3 position )
    {
        position.x = 0.5f + Utils.Fastfloor(position.x);
        position.z = 0.5f + Utils.Fastfloor(position.z);
        position.y = Utils.Fastfloor(position.y);
        if (!this.PatrolCoordinates.Contains(position))
            this.PatrolCoordinates.Add(position);
    }

    // Reads the buff and quest information
    public override void Read(byte _version, BinaryReader _br)
    {
        base.Read(_version, _br);
        this.strMyName = _br.ReadString();
        this.Buffs.Read(_br);
        this.QuestJournal = new QuestJournal();
        this.QuestJournal.Read(_br);
        this.PatrolCoordinates.Clear();
        String strPatrol = _br.ReadString();
        foreach (String strPatrolPoint in strPatrol.Split(';'))
        {
            Vector3 temp = StringToVector3(strPatrolPoint);
            if (temp != Vector3.zero)
                this.PatrolCoordinates.Add(temp);
        }

        if (this.PatrolCoordinates.Count > 0)
            this.Buffs.AddCustomVar("CurrentOrder", (float)Orders.Patrol);
        else
            this.Buffs.AddCustomVar("CurrentOrder", (float)Orders.Wander);

        

    }

    public Vector3 StringToVector3(string sVector)
    {
        if (String.IsNullOrEmpty(sVector))
            return Vector3.zero;

        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            sVector = sVector.Substring(1, sVector.Length - 2);

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

    // Saves the buff and quest information
    public override void Write(BinaryWriter _bw)
    {
        base.Write(_bw);
        _bw.Write(this.strMyName);
        this.Buffs.Write(_bw, true);
        this.QuestJournal.Write(_bw);
        String strPatrolCoordinates ="";
        foreach (Vector3 temp in this.PatrolCoordinates)
            strPatrolCoordinates += ";" + temp;

        _bw.Write(strPatrolCoordinates);
    }

    public void DisplayStats()
    {
        DisplayLog(ToString());
    }

    public override string ToString()
    {
        String FoodAmount = ((float)Mathf.RoundToInt(this.Stats.Stamina.ModifiedMax + this.Stats.Entity.Buffs.GetCustomVar("foodAmount"))).ToString() ;
        String WaterAmount = ((float)Mathf.RoundToInt(this.Stats.Water.Value + this.Stats.Entity.Buffs.GetCustomVar("waterAmount"))).ToString();
        String strSanitation = "Disabled.";
        if (this.Buffs.HasCustomVar("solidWasteAmount"))
            strSanitation = this.Buffs.GetCustomVar("solidWasteAmount").ToString();

        string strOutput = this.strMyName + " The " + this.entityName + " - ID: " + this.entityId + " Health: " + this.Stats.Health.Value;
        strOutput += " Stamina: " + this.Stats.Stamina.Value + " Thirst: " + this.Stats.Water.Value + " Food: " + FoodAmount + " Water: " + WaterAmount;
        strOutput += " Sanitation: " + strSanitation;

        if (this.Buffs.HasCustomVar("CurrentOrder"))
            strOutput += " Current Order: " + (Orders)(int)this.Buffs.GetCustomVar("CurrentOrder");

        strOutput += "\n Active Buffs: ";
        foreach (BuffValue buff in this.Buffs.ActiveBuffs)
            strOutput += "\n\t" + buff.BuffName + " ( Seconds: " + buff.DurationInSeconds + " Ticks: " + buff.DurationInTicks + " )";

        strOutput += "\n Active Quests: ";
        foreach (Quest quest in this.QuestJournal.quests)
            strOutput += "\n\t" + quest.ID + " Current State: " + quest.CurrentState + " Current Phase: " + quest.CurrentPhase;

        strOutput += "\n Patrol Points: ";
        foreach (Vector3 vec in this.PatrolCoordinates)
            strOutput += "\n\t" + vec.ToString();

        strOutput += "\n\nCurrency: " + this.HireCurrency + " Count: " + this.inventory.GetItemCount(this.HireCurrency, false, -1, -1).ToString();
        return strOutput;
    }

    public void GiveQuest(String strQuest)
    {
        // Don't give duplicate quests.
        foreach (Quest quest in this.QuestJournal.quests)
        {
            if (quest.ID == strQuest.ToLower() )
                return;
        }

        // Make sure the quest is valid
        Quest NewQuest = QuestClass.CreateQuest(strQuest);
        if (NewQuest == null)
            return;

        // If there's no shared owner, it tries to read the PlayerLocal's entity ID. This entity doesn't have that.
        NewQuest.SharedOwnerID = this.entityId;
        NewQuest.QuestGiverID = -1;
        this.QuestJournal.AddQuest(NewQuest);
    }

    protected virtual void SetupStartingItems()
    {
        for (int i = 0; i < this.itemsOnEnterGame.Count; i++)
        {
            ItemStack itemStack = this.itemsOnEnterGame[i];
            ItemClass forId = ItemClass.GetForId(itemStack.itemValue.type);
            if (forId.HasQuality)
                itemStack.itemValue = new ItemValue(itemStack.itemValue.type, 1, 6, false, default(FastTags), 1f);
            else
                itemStack.count = forId.Stacknumber.Value;
            this.inventory.SetItem(i, itemStack);
        }
    }
   
    public override void OnUpdateLive()
    {
        // Non-player entities don't fire all the buffs or stats, so we'll manually fire the water tick,
        this.Stats.Water.Tick(0.5f, 0, false);

        // then fire the updatestats over time, which is protected from a IsPlayer check in the base onUpdateLive().
        this.Stats.UpdateStatsOverTime(0.5f);

        // Make the entity sensitive to the environment.
        this.Stats.UpdateWeatherStats(0.5f, this.world.worldTime, false);

        // Check if there's a player within 10 meters of us. If not, resume wandering.
        this.emodel.avatarController.SetBool("IsBusy", false);

        List<global::Entity> entitiesInBounds = global::GameManager.Instance.World.GetEntitiesInBounds(this, new Bounds(this.position, Vector3.one * 3f));
        if (entitiesInBounds.Count > 0)
        {
            for (int i = 0; i < entitiesInBounds.Count; i++)
            {
                if (entitiesInBounds[i] is EntityPlayer)
                    this.emodel.avatarController.SetBool("IsBusy", true);
            }

        }


        // Check the state to see if the controller IsBusy or not. If it's not, then let it walk.
        bool isBusy = false;
        this.emodel.avatarController.TryGetBool("IsBusy", out isBusy);

        if (IsAlert)
            isBusy = false;

        if (isBusy == false)
            base.OnUpdateLive();
      
    }



    protected override void updateSpeedForwardAndStrafe(Vector3 _dist, float _partialTicks)
    {
        if (this.isEntityRemote && _partialTicks > 1f)
        {
            _dist /= _partialTicks;
        }
        this.speedForward *= 0.5f;
        this.speedStrafe *= 0.5f;
        this.speedVertical *= 0.5f;
        if (Mathf.Abs(_dist.x) > 0.001f || Mathf.Abs(_dist.z) > 0.001f)
        {
            float num = Mathf.Sin(-this.rotation.y * 3.14159274f / 180f);
            float num2 = Mathf.Cos(-this.rotation.y * 3.14159274f / 180f);
            this.speedForward += num2 * _dist.z - num * _dist.x;
            this.speedStrafe += num2 * _dist.x + num * _dist.z;
        }
        if (Mathf.Abs(_dist.y) > 0.001f)
        {
            this.speedVertical += _dist.y;
        }
        this.SetMovementState();
    }
}

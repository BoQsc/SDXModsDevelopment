﻿using System;
using UnityEngine;

//<property name="AITask-5" value="ApproachAndAttackTargetSDX, Mods" param1="" param2=""  /> <!-- param1 not used -->
// Disables the Eating animation
class EAIApproachAndAttackSDX : EAIApproachAndAttackTarget
{
    private bool isTargetToEat = false;
    private Vector3 entityTargetPos;

    // Token: 0x04002282 RID: 8834
    private Vector3 entityTargetVel;
    private bool isGoingHome;

    // Token: 0x0400227F RID: 8831
    private float homeTimeout;
    private bool hasHome;
    private bool isEating;
    private int pathCounter;
    private int attackTimeout;
    private int relocateTicks;
    private float maxChaseTime;

    public override void Start()
    {
        this.entityTargetPos = this.entityTarget.position;
        this.entityTargetVel = Vector3.zero;
        this.isTargetToEat = false;
        this.isEating = false;
        this.theEntity.IsEating = false;
        this.homeTimeout = ((!this.theEntity.IsSleeper) ? this.maxChaseTime : 90f);
        this.hasHome = (this.homeTimeout > 0f);
        this.isGoingHome = false;
        if (this.theEntity.ChaseReturnLocation == Vector3.zero)
              this.theEntity.ChaseReturnLocation = ((!this.theEntity.IsSleeper) ? this.theEntity.position : this.theEntity.SleeperSpawnPosition);

        this.pathCounter = 0;
        this.relocateTicks = 0;
  
        this.attackTimeout = 5;
    }
    public override bool Continue()
    {
        if (this.theEntity.sleepingOrWakingUp || this.theEntity.bodyDamage.CurrentStun != global::EnumEntityStunType.None)
        {
            return false;
        }
        EntityAlive attackTarget = this.theEntity.GetAttackTarget();

        if (attackTarget)
            if (!attackTarget.IsAlive())
                return false;

        if (this.isGoingHome)
        {
            return !attackTarget && this.theEntity.ChaseReturnLocation != Vector3.zero;
        }
        return attackTarget && !(attackTarget != this.entityTarget) ;
    }
}


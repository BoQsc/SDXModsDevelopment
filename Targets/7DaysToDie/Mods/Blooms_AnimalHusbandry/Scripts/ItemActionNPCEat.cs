﻿using System;
using UnityEngine;
class ItemActionExchangeItemSDX: ItemActionExchangeItem
{

    public bool isFocusingBlock(WorldRayHitInfo _hitInfo)
    {
        for (int i = 0; i < this.focusedBlocks.Count; i++)
        {
            BlockValue other = this.focusedBlocks[i];
            if (_hitInfo.hit.blockValue.Equals(other))
                 return true;
        }
        return false;
    }

    public override void ExecuteAction(ItemActionData _actionData, bool _bReleased)
    {
        if (!_bReleased)
            return;

        if (Time.time - _actionData.lastUseTime < this.Delay)
            return;

        ItemInventoryData invData = _actionData.invData;

        // Create a new ray based on the entity's current look vector.
        Ray lookRay = new Ray(invData.holdingEntity.position, invData.holdingEntity.GetLookVector());
        if (!Voxel.Raycast(invData.world, lookRay, Constants.cDigAndBuildDistance, -538480645, 4095, 0f))
            return;

        if (Voxel.voxelRayHitInfo.bHitValid && this.isFocusingBlock(Voxel.voxelRayHitInfo))
        {
            Debug.Log("Execute Action: " + this.GetType() + " Index Action ID: " + _actionData.indexInEntityOfAction + " Look Ray: " + lookRay + " Valid Hit? " + Voxel.voxelRayHitInfo.bHitValid + " focused block:" + Voxel.voxelRayHitInfo.hit.blockValue.Block.GetBlockName());

            this.hitLiquidBlock = Voxel.voxelRayHitInfo.hit.blockValue;
            this.hitLiquidPos = Voxel.voxelRayHitInfo.hit.blockPos;
            _actionData.lastUseTime = Time.time;
            invData.holdingEntity.RightArmAnimationUse = true;
            if (this.soundStart != null)
                invData.holdingEntity.PlayOneShot(this.soundStart);

           // Debug.Log("Before Execution Action: " + _actionData.invData.holdingEntity.ToString());
            if (_actionData.indexInEntityOfAction == 0)
            {
             //   Debug.Log("Eating " + this.hitLiquidBlock.Block.GetBlockName() );
                _actionData.invData.holdingEntity.FireEvent(MinEventTypes.onSelfPrimaryActionStart);
                _actionData.invData.holdingEntity.FireEvent(MinEventTypes.onSelfPrimaryActionEnd);

            }
            else
            {
             //   Debug.Log("Drinking " + this.hitLiquidBlock.Block.GetBlockName() );
                _actionData.invData.holdingEntity.FireEvent(MinEventTypes.onSelfSecondaryActionStart);
                _actionData.invData.holdingEntity.FireEvent(MinEventTypes.onSelfSecondaryActionEnd);

            }
           // Debug.Log("AFter Execution Action: " + _actionData.invData.holdingEntity.ToString());
        }
    
    }

}


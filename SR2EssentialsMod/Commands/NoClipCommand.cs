﻿using Il2Cpp;
using Il2CppMonomiPark.KFC;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController.MovementAndLookTypes;
using UnityEngine;

namespace SR2E.Commands
{
    //NOT WORKING YET
    public class NoClipCommand : SR2CCommand
    {
        public override string ID { get; } = "noclip";
        public override string Usage { get; } = "noclip";
        public override string Description { get; } = "Toggles noclip";
        
        public override bool Execute(string[] args)
        {
            if (args != null)
            {
                SR2Console.SendError($"The '<color=white>{ID}</color>' command takes no arguments");
                return false;
            }

            
            if (SceneContext.Instance == null)
            { SR2Console.SendError("Load a save first!"); return false; }
                
            if (SceneContext.Instance.PlayerState == null) 
            { SR2Console.SendError("Load a save first!"); return false; }

            
            KFCCharacterController con = Object.FindObjectOfType<KFCCharacterController>();
            con._parameters._defaultMovementAndLookType = new FreeflyMovementAndLookType();
            
            /*
            bool noclipState = SceneContext.Instance.Player.GetComponentInChildren<vp_FPController>().MotorFreeFly;
            SceneContext.Instance.Player.GetComponentInChildren<vp_FPController>().MotorFreeFly = !noclipState;
            if (!noclipState) originalLayer = SceneContext.Instance.Player.layer;
            SceneContext.Instance.Player.layer = noclipState ? (originalLayer) : LayerMask.NameToLayer("RaycastOnly");
            
            if (noclipState)
            {
                SR2Console.SendMessage("Successfully disabled noclip");
            }
            else
            {
                SR2Console.SendMessage("Successfully enabled noclip");
            }*/
            

            return true;
        }
        public int originalLayer;
    }
}
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Valve.VR
{
    using System;
    using UnityEngine;
    
    
    public partial class SteamVR_Actions
    {
        
        private static SteamVR_Input_ActionSet_default p__default;
        
        private static SteamVR_Input_ActionSet_startview p_startview;
        
        private static SteamVR_Input_ActionSet_pointcloudview p_pointcloudview;
        
        public static SteamVR_Input_ActionSet_default _default
        {
            get
            {
                return SteamVR_Actions.p__default.GetCopy<SteamVR_Input_ActionSet_default>();
            }
        }
        
        public static SteamVR_Input_ActionSet_startview startview
        {
            get
            {
                return SteamVR_Actions.p_startview.GetCopy<SteamVR_Input_ActionSet_startview>();
            }
        }
        
        public static SteamVR_Input_ActionSet_pointcloudview pointcloudview
        {
            get
            {
                return SteamVR_Actions.p_pointcloudview.GetCopy<SteamVR_Input_ActionSet_pointcloudview>();
            }
        }
        
        private static void StartPreInitActionSets()
        {
            SteamVR_Actions.p__default = ((SteamVR_Input_ActionSet_default)(SteamVR_ActionSet.Create<SteamVR_Input_ActionSet_default>("/actions/default")));
            SteamVR_Actions.p_startview = ((SteamVR_Input_ActionSet_startview)(SteamVR_ActionSet.Create<SteamVR_Input_ActionSet_startview>("/actions/startview")));
            SteamVR_Actions.p_pointcloudview = ((SteamVR_Input_ActionSet_pointcloudview)(SteamVR_ActionSet.Create<SteamVR_Input_ActionSet_pointcloudview>("/actions/pointcloudview")));
            Valve.VR.SteamVR_Input.actionSets = new Valve.VR.SteamVR_ActionSet[] {
                    SteamVR_Actions._default,
                    SteamVR_Actions.startview,
                    SteamVR_Actions.pointcloudview};
        }
    }
}
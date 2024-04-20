using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Unity.VisualScripting;
using UnityEngine;

public static class ControllerMessages
{
    //Event to send messages back to the game controller state machine
    public static EventHandler<ControllerMessage> OnControllerMessage;
    public class ControllerMessage : EventArgs
    {
        public string JSONMessage;
    }


    //Event to send messages to update the screen UI
    public static EventHandler<UIControllerMessage> OnUIControllerMessage;
    public class UIControllerMessage : EventArgs
    {
        public string JSONMessage;
    }
}

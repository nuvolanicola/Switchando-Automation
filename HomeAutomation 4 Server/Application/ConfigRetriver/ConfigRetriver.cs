﻿using HomeAutomation.Network;
using HomeAutomation.Objects;
using HomeAutomation.Objects.Fans;
using HomeAutomation.Objects.Inputs;
using HomeAutomation.Objects.Lights;
using HomeAutomation.Objects.Switches;
using HomeAutomation.Rooms;
using HomeAutomationCore;
using HomeAutomationCore.Client;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;

namespace HomeAutomation.ConfigRetriver
{
    public class ConfigRetriver
    {
        public static void Update()
        {
            string json = JsonConvert.SerializeObject(HomeAutomationServer.server.Rooms);
            File.WriteAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/configuration.json", json);
        }
        public ConfigRetriver()
        {
            foreach (NetworkInterface netInt in HomeAutomationServer.server.NetworkInterfaces)
            {
                if (netInt.Id.Equals("configuration")) return;
            }
            NetworkInterface.Delegate requestHandler;
            requestHandler = SendParameters;
            NetworkInterface networkInterface = new NetworkInterface("configuration", requestHandler);
        }
        public void SendParameters(string[] request)
        {
            foreach (string cmd in request)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "addroom":
                        CreateRoom(request);
                        break;

                    case "removeroom":
                        RemoveRoom(request);
                        break;
                    case "removeobject":
                        RemoveObject(request);
                        break;
                    case "removeclient":
                        RemoveClient(request);
                        break;

                    case "addclient":
                        CreateClient(request);
                        break;

                    case "addlightrgb":
                        CreateLightRGB(request);
                        break;

                    case "addlightw":
                        CreateLightW(request);
                        break;

                    case "addsimplefan":
                        CreateSimpleFan(request);
                        break;

                    case "addrelay":
                        CreateRelay(request);
                        break;

                    case "addbutton":
                        CreateButton(request);
                        break;

                    case "buttonaddcommand":
                        ButtonAddCommand(request);
                        break;
                    case "buttonaddobject":
                        ButtonAddObject(request);
                        break;

                    case "addswitchbutton":
                        CreateSwitchButton(request);
                        break;

                    case "switchbuttonaddcommand":
                        SwitchButtonAddCommand(request);
                        break;
                    case "switchbuttoaddobject":
                        SwitchButtonAddObject(request);
                        break;

                    case "updatefile":
                        break;
                }
            }
            string json = JsonConvert.SerializeObject(HomeAutomationServer.server.Rooms);
            File.WriteAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/configuration.json", json);
        }
        private void CreateClient(string[] data)
        {
            string name = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "addclient":
                        name = command[1];
                        break;
                }
            }

            foreach (Client clnt in HomeAutomationServer.server.Clients)
            {
                if (clnt.Name.ToLower().Equals(name.ToLower())) return;
            }
            new Client(null, 0, name);
        }
        private void RemoveRoom(string[] data)
        {
            string name = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "removeroom":
                        name = command[1];
                        break;
                }
            }
            foreach (Room room in HomeAutomationServer.server.Rooms)
            {
                if (room.Name.Equals(name))
                {
                    HomeAutomationServer.server.Rooms.Remove(room);
                    return;
                }
            }
        }
        private void RemoveObject(string[] data)
        {
            string name = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "removeobject":
                        name = command[1];
                        break;
                }
            }
            foreach (Room room in HomeAutomationServer.server.Rooms)
            {
                foreach (IObject iobj in room.Objects)
                {
                    if (iobj.GetName().Equals(name))
                    {
                        room.Objects.Remove(iobj);
                        return;
                    }
                }
            }
            foreach (IObject iobj in HomeAutomationServer.server.Objects)
            {
                if (iobj.GetName().Equals(name))
                {
                    HomeAutomationServer.server.Objects.Remove(iobj);
                    return;
                }
            }
        }
        private void RemoveClient(string[] data)
        {
            string name = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "removeclient":
                        name = command[1];
                        break;
                }
            }
            foreach (Client client in HomeAutomationServer.server.Clients)
            {
                if (client.Name.Equals(name))
                {
                    HomeAutomationServer.server.Clients.Remove(client);
                    return;
                }
            }
        }
        private void CreateRoom(string[] data)
        {
            string name = null;
            bool hiddenRoom = false;
            string[] friendlyNames = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "addroom":
                        name = command[1];
                        break;

                    case "setfriendlynames":
                        string names = command[1];
                        friendlyNames = names.Split(',');
                        break;

                    case "hiddenroom":
                        string hiddenroomString = command[1];
                        hiddenRoom = bool.Parse(hiddenroomString);
                        break;
                }
            }
            Room room = new Room(name, friendlyNames, hiddenRoom);
        }
        private void CreateLightRGB(string[] data)
        {
            string name = null;
            string[] friendlyNames = null;
            string description = null;

            uint pinR = 0;
            uint pinG = 0;
            uint pinB = 0;

            Client client = null;

            Room room = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "addlightrgb":
                        name = command[1];
                        break;
                    case "description":
                        description = command[1];
                        break;
                    case "setfriendlynames":
                        string names = command[1];
                        friendlyNames = names.Split(',');
                        break;

                    case "addpinr":
                        string pinr = command[1];
                        pinR = uint.Parse(pinr);
                        break;
                    case "addping":
                        string ping = command[1];
                        pinG = uint.Parse(ping);
                        break;
                    case "addpinb":
                        string pinb = command[1];
                        pinB = uint.Parse(pinb);
                        break;

                    case "client":
                        string clientName = command[1];
                        foreach (Client clnt in HomeAutomationServer.server.Clients)
                        {
                            if (clnt.Name.Equals(clientName))
                            {
                                client = clnt;
                            }
                        }
                        if (client == null) return;
                        break;

                    case "room":
                        foreach (Room stanza in HomeAutomationServer.server.Rooms)
                        {
                            if (stanza.Name.ToLower().Equals(command[1].ToLower()))
                            {
                                room = stanza;
                            }
                        }
                        break;
                }
            }
            if (room == null) return;
            RGBLight light = new RGBLight(client, name, pinR, pinG, pinB, description, friendlyNames);
            room.AddItem(light);
        }
        private void CreateLightW(string[] data)
        {
            string name = null;
            string[] friendlyNames = null;
            string description = null;

            uint pin = 0;

            Client client = null;

            Room room = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "addlightw":
                        name = command[1];
                        break;
                    case "description":
                        description = command[1];
                        break;
                    case "setfriendlynames":
                        string names = command[1];
                        friendlyNames = names.Split(',');
                        break;

                    case "addpin":
                        string pinStr = command[1];
                        pin = uint.Parse(pinStr);
                        break;

                    case "client":
                        string clientName = command[1];
                        foreach (Client clnt in HomeAutomationServer.server.Clients)
                        {
                            if (clnt.Name.Equals(clientName))
                            {
                                client = clnt;
                            }
                        }
                        if (client == null) return;
                        break;

                    case "room":
                        foreach (Room stanza in HomeAutomationServer.server.Rooms)
                        {
                            if (stanza.Name.ToLower().Equals(command[1].ToLower()))
                            {
                                room = stanza;
                            }
                        }
                        break;
                }
            }
            if (room == null) return;
            WLight light = new WLight(client, name, pin, description, friendlyNames);
            room.AddItem(light);
        }
        private void CreateRelay(string[] data)
        {
            string name = null;
            string[] friendlyNames = null;
            string description = null;

            uint pin = 0;

            Client client = null;

            Room room = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "addrelay":
                        name = command[1];
                        break;
                    case "description":
                        description = command[1];
                        break;
                    case "setfriendlynames":
                        string names = command[1];
                        friendlyNames = names.Split(',');
                        break;

                    case "addpin":
                        string pinStr = command[1];
                        pin = uint.Parse(pinStr);
                        break;

                    case "client":
                        string clientName = command[1];
                        foreach (Client clnt in HomeAutomationServer.server.Clients)
                        {
                            if (clnt.Name.Equals(clientName))
                            {
                                client = clnt;
                            }
                        }
                        if (client == null) return;
                        break;

                    case "room":
                        foreach (Room stanza in HomeAutomationServer.server.Rooms)
                        {
                            if (stanza.Name.ToLower().Equals(command[1].ToLower()))
                            {
                                room = stanza;
                            }
                        }
                        break;
                }
            }
            if (room == null) return;
            Relay relay = new Relay(client, name, pin, description, friendlyNames);
            room.AddItem(relay);
        }
        private void CreateSimpleFan(string[] data)
        {
            string name = null;
            string[] friendlyNames = null;
            string description = null;

            uint pin = 0;

            Client client = null;

            Room room = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "addsimplefan":
                        name = command[1];
                        break;
                    case "description":
                        description = command[1];
                        break;
                    case "setfriendlynames":
                        string names = command[1];
                        friendlyNames = names.Split(',');
                        break;

                    case "addpin":
                        string pinStr = command[1];
                        pin = uint.Parse(pinStr);
                        break;

                    case "client":
                        string clientName = command[1];
                        foreach (Client clnt in HomeAutomationServer.server.Clients)
                        {
                            if (clnt.Name.Equals(clientName))
                            {
                                client = clnt;
                            }
                        }
                        if (client == null) return;
                        break;

                    case "room":
                        foreach (Room stanza in HomeAutomationServer.server.Rooms)
                        {
                            if (stanza.Name.ToLower().Equals(command[1].ToLower()))
                            {
                                room = stanza;
                            }
                        }
                        break;
                }
            }
            if (room == null) return;
            SimpleFan relay = new SimpleFan(client, name, pin, description, friendlyNames);
            room.AddItem(relay);
        }
        private void CreateButton(string[] data)
        {
            string name = null;
            uint pin = 0;
            Client client = null;

            Room room = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "addbutton":
                        name = command[1];
                        break;

                    case "addpin":
                        string pinStr = command[1];
                        pin = uint.Parse(pinStr);
                        break;

                    case "client":
                        string clientName = command[1];
                        foreach (Client clnt in HomeAutomationServer.server.Clients)
                        {
                            if (clnt.Name.Equals(clientName))
                            {
                                client = clnt;
                            }
                        }
                        if (client == null) return;
                        break;

                    case "room":
                        foreach (Room stanza in HomeAutomationServer.server.Rooms)
                        {
                            if (stanza.Name.ToLower().Equals(command[1].ToLower()))
                            {
                                room = stanza;
                            }
                        }
                        break;
                }
            }
            if (room == null) return;
            Button button = new Button(client, name, pin);
            room.AddItem(button);
        }
        private void CreateSwitchButton(string[] data)
        {
            string name = null;
            uint pin = 0;
            Client client = null;

            Room room = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "addswitchbutton":
                        name = command[1];
                        break;

                    case "addpin":
                        string pinStr = command[1];
                        pin = uint.Parse(pinStr);
                        break;

                    case "client":
                        string clientName = command[1];
                        foreach (Client clnt in HomeAutomationServer.server.Clients)
                        {
                            if (clnt.Name.Equals(clientName))
                            {
                                client = clnt;
                            }
                        }
                        if (client == null) return;
                        break;

                    case "room":
                        foreach (Room stanza in HomeAutomationServer.server.Rooms)
                        {
                            if (stanza.Name.ToLower().Equals(command[1].ToLower()))
                            {
                                room = stanza;
                            }
                        }
                        break;
                }
            }
            if (room == null) return;
            SwitchButton button = new SwitchButton(client, name, pin);
            room.AddItem(button);
        }
        private void SwitchButtonAddCommand(string[] data)
        {
            string name = null;
            bool type = false;
            string newCmd = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "switchbuttonaddcommand":
                        name = command[1];
                        break;

                    case "type":
                        string typeStr = command[1];
                        if (typeStr.ToLower().Equals("on")) type = true; else type = false;
                        break;

                    case "command":
                        newCmd = command[1];
                        break;
                }
            }

            newCmd = newCmd.Replace(",,,", "&");
            newCmd = newCmd.Replace(",,", "=");

            foreach (IObject iobj in HomeAutomationServer.server.Objects)
            {
                if (iobj.GetObjectType() == HomeAutomationObject.SWITCH_BUTTON)
                {
                    if (iobj.GetName().Equals(name))
                    {
                        SwitchButton button = (SwitchButton)iobj;
                        button.AddCommand(newCmd, type);
                    }
                }
            }
        }
        private void ButtonAddCommand(string[] data)
        {
            string name = null;
            string newCmd = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "buttonaddcommand":
                        name = command[1];
                        break;

                    case "command":
                        newCmd = command[1];
                        break;
                }
            }

            foreach (IObject iobj in HomeAutomationServer.server.Objects)
            {
                if (iobj.GetObjectType() == HomeAutomationObject.BUTTON)
                {
                    if (iobj.GetName().Equals(name))
                    {
                        Button button = (Button)iobj;
                        button.AddCommand(newCmd);
                    }
                }
            }
        }
        private void ButtonAddObject(string[] data)
        {
            string name = null;
            string obj = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "buttonaddobject":
                        name = command[1];
                        break;

                    case "object":
                        obj = command[1];
                        break;
                }
            }

            if (name == null || obj == null) return;

            Button button = null;
            ISwitch switchable = null;

            foreach (IObject iobj in HomeAutomationServer.server.Objects)
            {
                if (iobj is ISwitch)
                {
                    if (iobj.GetName().Equals(obj))
                    {
                        switchable = (ISwitch)iobj;
                    }
                }
                if (iobj.GetObjectType() == HomeAutomationObject.BUTTON)
                {
                    if (iobj.GetName().Equals(name))
                    {
                        button = (Button)iobj;
                    }
                }
            }

            if (button == null || switchable == null) return;

            button.AddObject(switchable);
        }
        private void SwitchButtonAddObject(string[] data)
        {
            string name = null;
            string obj = null;

            foreach (string cmd in data)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "switchbuttonaddobject":
                        name = command[1];
                        break;

                    case "object":
                        obj = command[1];
                        break;
                }
            }

            if (name == null || obj == null) return;

            SwitchButton button = null;
            ISwitch switchable = null;

            foreach (IObject iobj in HomeAutomationServer.server.Objects)
            {
                if (iobj is ISwitch)
                {
                    if (iobj.GetName().Equals(name))
                    {
                        switchable = (ISwitch)iobj;
                    }
                }
                if (iobj.GetObjectType() == HomeAutomationObject.SWITCH_BUTTON)
                {
                    if (iobj.GetName().Equals(name))
                    {
                        button = (SwitchButton)iobj;
                    }
                }
            }

            if (button == null || switchable == null) return;

            button.AddObject(switchable);
        }
    }
}
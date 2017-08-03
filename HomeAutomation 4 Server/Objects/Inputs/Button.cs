﻿using Homeautomation.GPIO;
using HomeAutomation.Network;
using HomeAutomation.Objects.Switches;
using HomeAutomationCore;
using HomeAutomationCore.Client;
using System;
using System.Collections.Generic;
using System.Timers;

namespace HomeAutomation.Objects.Inputs
{
    class Button : IObject
    {
        Client Client;
        public string ClientName;

        public string Name;

        public uint Pin;
        bool Status;
        bool EmulatedSwitchStatus;
        bool IsRemote;

        Timer timer;

        public List<string> Commands;
        public List<string> Objects;

        public HomeAutomationObject ObjectType = HomeAutomationObject.BUTTON;
        public Button(Client client, string name, bool isRemote)
        {
            new Button(client, name, 0, isRemote);
        }
        public Button(Client client, string name, uint pin, bool isRemote)
        {
            this.Client = client;
            this.ClientName = client.Name;
            this.Pin = pin;
            this.Name = name;
            this.IsRemote = isRemote;

            this.Commands = new List<string>();
            this.Objects = new List<string>();

            if (isRemote)
            {

            }
            else
            {
                if (Client.Name.Equals("local"))
                {
                    PIGPIO.set_pull_up_down(0, this.Pin, 2);
                    Console.WriteLine("PUD-UP was set on GPIO" + this.Pin);

                    timer = new Timer();

                    timer.Elapsed += Tick;
                    timer.Interval = 200;
                    timer.Start();
                }
            }

            foreach (NetworkInterface netInt in HomeAutomationServer.server.NetworkInterfaces)
            {
                if (netInt.Id.Equals("button")) return;
            }
            NetworkInterface.Delegate requestHandler;
            requestHandler = SendParameters;
            NetworkInterface networkInterface = new NetworkInterface("button", requestHandler);

            HomeAutomationServer.server.Objects.Add(this);
        }
        public void SetClient(Client client)
        {
            this.Client = client;
        }

        public void AddCommand(string command)
        {
            Commands.Add(command.Replace("=", ",,").Replace("&", ",,,"));
        }
        public void RemoveCommand(string command)
        {
            Commands.Remove(command.Replace("=", ",,").Replace("&", ",,,"));
        }
        public void AddObject(ISwitch obj)
        {
            if (!Objects.Contains(obj.GetName()))
                Objects.Add(obj.GetName());
        }
        public void RemoveObject(ISwitch obj)
        {
            if (Objects.Contains(obj.GetName()))
                Objects.Remove(obj.GetName());
        }
        public void AddObject(string obj)
        {
            if (!Objects.Contains(obj))
                Objects.Add(obj);
        }
        public void RemoveObject(string obj)
        {
            if (Objects.Contains(obj))
                Objects.Remove(obj);
        }
        public void Tick(object sender, ElapsedEventArgs args)
        {
            int currentStatus = PIGPIO.gpio_read(0, Pin);
            bool lStatus;
            if (currentStatus == 1) lStatus = false; else lStatus = true;

            if (lStatus == this.Status) return;
            else
            {
                this.Status = lStatus;
                StatusChanged(lStatus);
            }
        }
        public void StatusChanged(bool value)
        {
            Console.WriteLine(this.Name + " status: " + this.Status);
            if (value)
            {
                HomeAutomationServer.server.Telegram.Log("Button `" + this.Name + "` has been pressed.");
                if (EmulatedSwitchStatus) EmulatedSwitchStatus = false; else EmulatedSwitchStatus = true;
                foreach (string command in Commands)
                {
                    var message = command.Replace("%EmulatedSwitchStatus%", EmulatedSwitchStatus.ToString());
                    message = message.Replace(",,,", "&");
                    message = message.Replace(",,", "=");
                    Console.WriteLine(message);
                    string[] commands = message.Split('&');

                    string[] icommand = commands[0].Split('=');
                    if (icommand[0].Equals("interface"))
                    {
                        foreach (NetworkInterface networkInterface in HomeAutomationServer.server.NetworkInterfaces)
                        {
                            if (networkInterface.Id.Equals(icommand[1]))
                            {
                                Console.WriteLine(commands[2]);
                                networkInterface.Run(commands);
                            }
                        }
                    }
                }
                List<ISwitch> objectsList = new List<ISwitch>();
                foreach (IObject iobj in HomeAutomationServer.server.Objects)
                {
                    if (this.Objects.Contains(iobj.GetName()))
                    {
                        objectsList.Add((ISwitch)iobj);
                    }
                }
                bool allOn = true;
                bool allOff = true;
                foreach (ISwitch iobj in objectsList)
                {
                    if (iobj.IsOn())
                    {
                        allOff = false;
                    }
                    else
                    {
                        allOn = false;
                    }
                }
                if (allOn) EmulatedSwitchStatus = false;
                if (allOff) EmulatedSwitchStatus = true;

                foreach (ISwitch iobj in objectsList)
                {
                    if (EmulatedSwitchStatus)
                    {
                        iobj.Start();
                    }
                    else
                    {
                        iobj.Stop();
                    }
                }
            }
        }
        public string GetName()
        {
            return this.Name;
        }
        public HomeAutomationObject GetObjectType()
        {
            return HomeAutomationObject.BUTTON;
        }
        public NetworkInterface GetInterface()
        {
            return null;
        }
        public string[] GetFriendlyNames()
        {
            return new string[0];
        }
        public static void SendParameters(string[] request)
        {
            Button button = null;
            foreach (string cmd in request)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "objname":
                        foreach (IObject obj in HomeAutomationServer.server.Objects)
                        {
                            if (obj.GetName().Equals(command[1]))
                            {
                                button = (Button)obj;
                            }
                            if (Array.IndexOf(obj.GetFriendlyNames(), command[1].ToLower()) > -1)
                            {
                                button = (Button)obj;
                            }
                        }
                        break;

                    case "switch":
                        button.StatusChanged(true);
                        break;
                }
            }
        }
    }
}

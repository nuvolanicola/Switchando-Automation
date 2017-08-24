﻿using HomeAutomation.Objects;
using HomeAutomation.Objects.Fans;
using HomeAutomation.Objects.Inputs;
using HomeAutomation.Objects.Lights;
using HomeAutomation.ServerRetriver;
using HomeAutomationCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeAutomation.Network
{
    public class TCPClient
    {
        TcpClient client;
        NetworkStream stream;
        public StreamWriter writer = null;

        public void StartClient(string ip)
        {
            HomeAutomationClient.client.TcpClient = this;
            try
            {
                client = new TcpClient(ip, 2345);
                stream = client.GetStream();
                writer = new StreamWriter(stream);
                writer.AutoFlush = true;
                writer.WriteLine("client-handshake=" + HomeAutomationClient.client.ClientName + "&password=" + HomeAutomationClient.client.Password);

                HomeAutomationClient.client.ConnectionEstabilished = true;

                while (client.Connected)
                {
                    NetworkStream stream = client.GetStream();
                    var reader = new StreamReader(stream);
                    string message = reader.ReadLine();
                    if (message == null) break;
                    Console.WriteLine("From TCP -> " + message);

                    string[] commands = message.Split('&');

                    string[] icommand = commands[0].Split('=');
                    if (icommand[0].Equals("interface"))
                    {
                        foreach (NetworkInterface networkInterface in HomeAutomationClient.client.NetworkInterfaces)
                        {
                            if (networkInterface.Id.Equals(icommand[1]))
                            {
                                networkInterface.Run(commands);
                            }
                        }
                    }
                    if (icommand[0].Equals("info_devices"))
                    {
                        //icommand[1] = icommand[1].Replace(",,", "=").Replace(",,,", "&");
                        HomeAutomationModel[] devices = JsonConvert.DeserializeObject<HomeAutomationModel[]>(icommand[1]);
                        HomeAutomationClient.client.Objects = new List<IObject>();
                        foreach (HomeAutomationModel device in devices)
                        {
                            if (!device.ClientName.Equals(HomeAutomationClient.client.ClientName))
                            {
                                Console.WriteLine("I'M NOT " + device.ClientName + "!");
                                continue;
                            }
                            Console.WriteLine(device.ClientName + " <<->> " + device.Name + " -> " + device.ObjectType.ToString());
                            if (device.ObjectType == HomeAutomationObject.LIGHT)
                            {
                                if (device.LightType == Objects.Lights.LightType.RGB_LIGHT)
                                {
                                    RGBLight light = new RGBLight();
                                    light.PinR = device.PinR;
                                    light.PinG = device.PinG;
                                    light.PinB = device.PinB;
                                    light.Name = device.Name;
                                    light.FriendlyNames = device.FriendlyNames;
                                    light.Description = device.Description;
                                    light.Switch = device.Switch;
                                    light.ValueR = device.ValueR;
                                    light.ValueG = device.ValueG;
                                    light.ValueB = device.ValueB;

                                    HomeAutomationClient.client.Objects.Add(light);
                                    light.Init();
                                }
                                else if (device.LightType == Objects.Lights.LightType.W_LIGHT)
                                {
                                    WLight light = new WLight();
                                    light.Pin = device.Pin;
                                    light.Name = device.Name;
                                    light.FriendlyNames = device.FriendlyNames;
                                    light.Description = device.Description;
                                    light.Switch = device.Switch;
                                    light.Value = device.Value;

                                    HomeAutomationClient.client.Objects.Add(light);
                                    light.Init();
                                }
                            }
                            else if (device.ObjectType == HomeAutomationObject.FAN)
                            {
                                SimpleFan fan = new SimpleFan();
                                fan.Pin = device.Pin;
                                fan.Name = device.Name;
                                fan.Description = device.Description;
                                fan.Enabled = device.Switch;

                                HomeAutomationClient.client.Objects.Add(fan);
                            }
                            else if (device.ObjectType == HomeAutomationObject.BUTTON)
                            {
                                Button button = new Button(device.Name, device.Pin);
                                foreach(string command in device.Commands)
                                {
                                    button.AddCommand(command);
                                }
                                foreach (string objectName in device.Objects)
                                {
                                    button.AddObject(objectName);
                                }
                            }
                            else if (device.ObjectType == HomeAutomationObject.SWITCH_BUTTON)
                            {
                                SwitchButton button = new SwitchButton(device.Name, device.Pin);
                                foreach (string command in device.CommandsOn)
                                {
                                    button.AddCommand(command, true);
                                }
                                foreach (string command in device.CommandsOff)
                                {
                                    button.AddCommand(command, false);
                                }
                                foreach (string objectName in device.Objects)
                                {
                                    button.AddObject(objectName);
                                }
                            }
                            else if (device.ObjectType == HomeAutomationObject.GENERIC_SWITCH)
                            {
                                Relay relay = new Relay();
                                relay.Pin = device.Pin;
                                relay.Name = device.Name;
                                relay.Description = device.Description;
                                relay.Enabled = device.Switch;

                                HomeAutomationClient.client.Objects.Add(relay);
                            }
                        }
                    }
                }
            }
            catch(SocketException)
            {
                Console.WriteLine("Can't connect to the server!");
                HomeAutomationClient.client.ConnectionEstabilished = false;
            }
            Thread.Sleep(10000);
            Console.WriteLine("Reconnecting...");
            StartClient(ip);
        }
    }
}

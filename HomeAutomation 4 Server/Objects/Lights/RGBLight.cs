﻿using Homeautomation.GPIO;
using HomeAutomation.Dictionaries;
using HomeAutomation.Network;
using HomeAutomationCore;
using HomeAutomationCore.Client;
using System;
using System.Diagnostics;
using System.Threading;

namespace HomeAutomation.Objects.Lights
{
    class RGBLight : IColorableLight
    {
        Client Client;
        public string ClientName;

        public uint PinR, PinG, PinB;
        public uint ValueR, ValueG, ValueB, Brightness;
        uint PauseR, PauseG, PauseB;
        public bool Switch;

        public string Name;
        public string[] FriendlyNames;
        public string Description;

        public bool nolog = false;

        Semaphore Semaphore;

        public HomeAutomationObject ObjectType = HomeAutomationObject.LIGHT;
        public LightType LightType = LightType.RGB_LIGHT;

        public RGBLight()
        {
            foreach (NetworkInterface netInt in HomeAutomationServer.server.NetworkInterfaces)
            {
                if (netInt.Id.Equals("light_rgb")) return;
            }
            NetworkInterface.Delegate requestHandler;
            requestHandler = SendParameters;
            NetworkInterface networkInterface = new NetworkInterface("light_rgb", requestHandler);
            this.Semaphore = new Semaphore(0, 1);
            Semaphore.Release();
        }
        public RGBLight(Client Client, string Name, uint PinR, uint PinG, uint PinB, string Description, string[] FriendlyNames)
        {
            this.Client = Client;
            this.ClientName = Client.Name;
            this.PinR = PinR;
            this.PinG = PinG;
            this.PinB = PinB;
            this.Brightness = 100;
            this.Switch = true;

            this.ValueR = 255;
            this.ValueG = 255;
            this.ValueB = 255;

            this.Name = Name;
            this.Description = Description;
            this.FriendlyNames = FriendlyNames;

            this.Semaphore = new Semaphore(0, 1);
            Semaphore.Release();

            HomeAutomationServer.server.Objects.Add(this);

            if (Client.Name.Equals("local"))
            {
                PIGPIO.set_PWM_dutycycle(Client.PigpioID, (uint)PinR, (uint)ValueR);
                PIGPIO.set_PWM_dutycycle(Client.PigpioID, (uint)PinG, (uint)ValueG);
                PIGPIO.set_PWM_dutycycle(Client.PigpioID, (uint)PinB, (uint)ValueB);

                PIGPIO.set_PWM_frequency(Client.PigpioID, PinR, 4000);
                PIGPIO.set_PWM_frequency(Client.PigpioID, PinG, 4000);
                PIGPIO.set_PWM_frequency(Client.PigpioID, PinB, 4000);
            }

            foreach (NetworkInterface netInt in HomeAutomationServer.server.NetworkInterfaces)
            {
                if (netInt.Id.Equals("light_rgb")) return;
            }
            NetworkInterface.Delegate requestHandler;
            requestHandler = SendParameters;
            NetworkInterface networkInterface = new NetworkInterface("light_rgb", requestHandler);
        }
        public void SetClient(Client client)
        {
            this.Client = client;
        }
        public void Set(uint ValueR, uint ValueG, uint ValueB, int DimmerIntervals)
        {
            Set(ValueR, ValueG, ValueB, DimmerIntervals, false);
        }
        public void Set(uint ValueR, uint ValueG, uint ValueB, int DimmerIntervals, bool nolog = false)
        {
            Console.WriteLine("Changing color of " + this.Name + " with a dimmer of " + DimmerIntervals + "ms.");
            if (!nolog) HomeAutomationServer.server.Telegram.Log("Changing color of " + this.Name + " with a dimmer of " + DimmerIntervals + "ms.");
            this.Brightness = 100;
            if (ValueR == this.ValueR && ValueG == this.ValueG && ValueB == this.ValueB)
            {
                return;
            }

            if (ValueR == 0 && ValueG == 0 && ValueB == 0) this.Switch = false;
            else this.Switch = true;

            if (!Client.Name.Equals("local"))
            {
                UploadValues(ValueR, ValueG, ValueB, DimmerIntervals);
                this.ValueR = ValueR;
                this.ValueG = ValueG;
                this.ValueB = ValueB;
                return;
            }

            if (DimmerIntervals == 0)
            {
                PIGPIO.set_PWM_dutycycle(Client.PigpioID, (uint)PinR, (uint)ValueR);
                PIGPIO.set_PWM_dutycycle(Client.PigpioID, (uint)PinG, (uint)ValueG);
                PIGPIO.set_PWM_dutycycle(Client.PigpioID, (uint)PinB, (uint)ValueB);
                this.ValueR = ValueR;
                this.ValueG = ValueG;
                this.ValueB = ValueB;
                return;
            }

            if (this.ValueR != ValueR)
            {
                Thread thread = new Thread(DimmerThread);
                int subtract = (int)this.ValueR - (int)ValueR;
                double[] values = new double[4];
                values[0] = this.ValueR;
                values[1] = ValueR;
                values[2] = PinR;
                values[3] = (DimmerIntervals / subtract);
                if (((this.ValueR) - ValueR) == 0) values[3] = 0;
                
                thread.Start(values);
            }

            if (this.ValueG != ValueG)
            {
                Thread thread = new Thread(DimmerThread);
                double[] values2 = new double[4];
                values2[0] = this.ValueG;
                values2[1] = ValueG;
                values2[2] = PinG;
                values2[3] = ((DimmerIntervals / (((int)this.ValueG) - (int)ValueG)));
                if (((this.ValueG) - ValueG) == 0) values2[3] = 0;
                
                thread.Start(values2);
            }

            if (this.ValueB != ValueB)
            {
                Thread thread = new Thread(DimmerThread);
                double[] values3 = new double[4];
                values3[0] = this.ValueB;
                values3[1] = ValueB;
                values3[2] = PinB;
                values3[3] = ((DimmerIntervals / (((int)this.ValueB) - (int)ValueB)));
                if (((this.ValueB) - ValueB) == 0) values3[3] = 0;
                
                thread.Start(values3);

                Console.WriteLine("Waiting for dimmer...");
                while (thread.IsAlive)
                {
                    Thread.Sleep(10);
                }
            }

            this.ValueR = ValueR;
            this.ValueG = ValueG;
            this.ValueB = ValueB;
        }

        public void Dimm(uint percentage, int dimmer)
        {
            uint R = ValueR;
            uint G = ValueG;
            uint B = ValueB;

            if (Brightness == 0)
            {
                Set(255, 255, 255, dimmer);
            }
            else
            {
                R = R * percentage / Brightness;
                G = G * percentage / Brightness;
                B = B * percentage / Brightness;
                Set(R, G, B, dimmer);
            }

            this.Brightness = percentage;
        }

        public void DimmerThread(object data)
        {
            //await Task.Delay(1);
            double[] values = (double[])data;
            int led = (int)values[2];
            if (values[3] < 0) values[3] *= -1;
            if (values[0] <= values[1])
            {
                for (double i = values[0]; i <= values[1]; i = i + 1)
                {
                    Semaphore.WaitOne();
                    PIGPIO.set_PWM_dutycycle(Client.PigpioID, (uint)led, (uint)i);
                    Semaphore.Release();
                    if (values[3] == 0) values[3] = 1;
                    Thread.Sleep((int)values[3]);
                }
            }
            else
            {
                for (double i = values[0]; i >= values[1]; i = i - 1)
                {
                    Semaphore.WaitOne();
                    PIGPIO.set_PWM_dutycycle(Client.PigpioID, (uint)led, (uint)i);
                    Semaphore.Release();
                    if (values[3] == 0) values[3] = 1;
                    Thread.Sleep((int)values[3]);
                }
            }
            PIGPIO.set_PWM_dutycycle(Client.PigpioID, (uint)led, (uint)values[1]);
        }
        void Block(long durationTicks)
        {
            Stopwatch sw;
            sw = Stopwatch.StartNew();
            int i = 0;

            while (sw.ElapsedTicks <= durationTicks)
            {
                if (sw.Elapsed.Ticks % 100 == 0)
                {
                    i++;
                }
            }
            sw.Stop();
        }

        public void Pause()
        {
            if (Switch)
            {
                //PauseR = ValueR;
                //PauseG = ValueG;
                //PauseB = ValueB;
                Set(0, 0, 0, 1000);
            }
            else
            {
                Set(255, 255, 255, 1000);
                /*if (PauseR == 0 && PauseG == 0 && PauseB == 0)
                    Set(255, 255, 255, 1000);
                else
                    Set(PauseR, PauseG, PauseB, 1000);*/
            }
        }

        public void Pause(bool status)
        {
            if (!status)
            {
                //PauseR = ValueR;
                //PauseG = ValueG;
                //PauseB = ValueB;
                Set(0, 0, 0, 1000);
            }
            else
            {
                Set(255, 255, 255, 1000);
                /*if (PauseR == 0 && PauseG == 0 && PauseB == 0)
                    Set(255, 255, 255, 1000);
                else
                    Set(PauseR, PauseG, PauseB, 1000);*/
            }
        }

        public void Start()
        {
            Pause(true);
        }
        public void Stop()
        {
            Pause(false);
        }
        public bool IsOn()
        {
            return Switch;
        }

        public LightType GetLightType()
        {
            return LightType.RGB_LIGHT;
        }
        public new HomeAutomationObject GetObjectType()
        {
            return HomeAutomationObject.LIGHT;
        }
        public string GetName()
        {
            return Name;
        }
        public string[] GetFriendlyNames()
        {
            return FriendlyNames;
        }
        public double[] GetValues()
        {
            return new double[3] { ValueR, ValueG, ValueB };
        }
        public NetworkInterface GetInterface()
        {
            return NetworkInterface.FromId("light_rgb");
        }
        public static void SendParameters(string[] request)
        {
            RGBLight light = null;
            uint R = 0;
            uint G = 0;
            uint B = 0;
            int dimmer = 0;
            bool nolog = false;
            string color = null;
            uint dimm_percentage = 400;
            string status = null;
            foreach (string cmd in request)
            {
                string[] command = cmd.Split('=');
                if (command[0].Equals("interface")) continue;
                switch (command[0])
                {
                    case "objname":
                        foreach (IObject obj in HomeAutomationServer.server.Objects)
                        {
                            if (obj.GetName().ToLower().Equals(command[1].ToLower()))
                            {
                                light = (RGBLight)obj;
                                break;
                            }
                            if (Array.IndexOf(obj.GetFriendlyNames(), command[1].ToLower()) > -1)
                            {
                                light = (RGBLight)obj;
                                break;
                            }
                        }
                        break;

                    case "R":
                        R = uint.Parse(command[1]);
                        break;

                    case "G":
                        G = uint.Parse(command[1]);
                        break;

                    case "B":
                        B = uint.Parse(command[1]);
                        break;

                    case "dimmer":
                        dimmer = int.Parse(command[1]);
                        break;

                    case "color":
                        color = command[1];
                        break;

                    case "percentage":
                        dimm_percentage = uint.Parse(command[1]);
                        break;

                    case "nolog":
                        nolog = true;
                        break;

                    case "switch":
                        status = command[1];
                        break;
                }
            }
            if (status != null)
            {
                light.Pause(bool.Parse(status));
                return;
            }
            if (color != null)
            {
                uint[] vls = ColorConverter.ConvertNameToRGB(color);
                light.Set(vls[0], vls[1], vls[2], dimmer);
                return;
            }
            if (dimm_percentage != 400)
            {
                light.Dimm(dimm_percentage, dimmer);
                return;
            }
            light.Set(R, G, B, dimmer, nolog);
            return;
        }
        void UploadValues(uint ValueR, uint ValueG, uint ValueB, int DimmerIntervals)
        {
            Client.Sendata("interface=light_rgb&objname=" + this.Name + "&R=" + ValueR.ToString() + "&G=" + ValueG.ToString() + "&B=" + ValueB.ToString() + "&dimmer=" + DimmerIntervals);
        }
        public void Init()
        {
            if (Client.Name.Equals("local"))
            {
                PIGPIO.set_PWM_frequency(0, PinR, 4000);
                PIGPIO.set_PWM_frequency(0, PinG, 4000);
                PIGPIO.set_PWM_frequency(0, PinB, 4000);

                PIGPIO.set_PWM_dutycycle(0, (uint)PinR, (uint)ValueR);
                PIGPIO.set_PWM_dutycycle(0, (uint)PinG, (uint)ValueG);
                PIGPIO.set_PWM_dutycycle(0, (uint)PinB, (uint)ValueB);
            }
        }
    }
}
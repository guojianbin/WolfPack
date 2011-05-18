﻿using System;
using System.Linq;
using Wolfpack.Agent.Profiles;
using Wolfpack.Core;
using Wolfpack.Core.Interfaces;
using Wolfpack.Core.Loaders;
using Topshelf;

namespace Wolfpack.Agent
{
    internal class Startup
    {
        private static void Main(string[] args)
        {
            try
            {
                // select a profile from the cmdline args /profile:[profile] switch
                CmdLine.Init(args);
                var profile = LoadProfile();
                var role = profile.Role;

                HostFactory.Run(
                    config =>
                        {
                            config.SetDisplayName("Wolfpack Agent");
                            config.SetServiceName("WolfpackAgent");
                            config.SetDescription("Wolfpack Agent Service");

                            string username;
                            string password;

                            if (CmdLine.Value(CmdLine.SwitchNames.Username, out username) &&
                                CmdLine.Value(CmdLine.SwitchNames.Password, out password))
                            {
                                Logger.Debug("Running As: {0}", username);
                                config.RunAs(username, password);
                            }

                            config.Service<IRolePlugin>(service =>
                                                            {
                                                                service.SetServiceName("Wolfpack");
                                                                service.ConstructUsing(factory => role);
                                                                service.WhenStarted(s => s.Start());
                                                                service.WhenStopped(s => s.Stop());
                                                            });

                            config.ApplyCommandLine(string.Join(" ", CmdLine.Expanded.ToArray()));
                        });
             
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                var msg = string.Format("Wolfpack System Failure. IncidentId:={0}; Details:={1}",
                    Guid.NewGuid(),
                    ex);

                Logger.Error(msg);
                Environment.ExitCode = 1;
            }
        }

        private static IRoleProfile LoadProfile()
        {
            string profileName;

            if (!CmdLine.Value(CmdLine.SwitchNames.Profile, out profileName))
            {
                profileName = typeof(DefaultAgentProfile).Name;
                Logger.Debug("/profile:[name] not specified, using default profile '{0}'", profileName);                
            }

            Logger.Debug("Attempting to locate agent profile: {0}", profileName);

            IRoleProfile profile;

            if (!ProfileLoader.Find(profileName, out profile))
                throw new TypeLoadException(string.Format("Unable to find profile: {0}", profileName));

            Logger.Debug("Loaded profile: {0}", profileName);
            return profile;
        }
    }
}
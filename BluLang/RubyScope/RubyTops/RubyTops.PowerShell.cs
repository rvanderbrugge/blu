﻿using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BluApi.Common;

namespace BluLang.RubyScope.RubyTops
{
    public partial class RubyTops
    {
        /// <summary>
        /// Now we have everything ready: It is a simple function to put PowerShell syntax together
        /// </summary>
        /// <returns>PowerShell as string</returns>
        // TODO: this function is monolithic, split it
        public string ToPowerShell()
        {
            string powerShell = String.Empty;
            string delayedNotifications = String.Empty;

            foreach (KeyValuePair<string, dynamic> resource in GeneratedDictionary["resources"])
            {
                string resourceUniqueName = resource.Key;
                string[] resourceUniqueNameSplit = Regex.Split(resource.Key, @"->");
                string resourceFunctionName = resourceUniqueNameSplit[0];
                string resourceNameParameter = resourceUniqueNameSplit[1].SingleQuote();

                Dictionary<string, string> parameters = resource.Value;
                Dictionary<string, dynamic> notifiers = GeneratedDictionary["notifiers"];
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

                powerShell += resourceFunctionName;
                powerShell += " -ResourceName " + resourceNameParameter + " `" + Environment.NewLine;

                string notification = String.Empty;

                foreach (KeyValuePair<string, string> parameter in parameters)
                {
                    switch (parameter.Key.ToUpper())
                    {
                        // Notifies are not handeled here
                        case "NOTIFIES":
                            notification += Environment.NewLine;
                            notification += "#  -Notifies: " + parameter.Value + Environment.NewLine;
                            notification += "#  (Notification are auto generated by RubyTops.)";
                            break;
                
                        // "data" is a PowerShell reserved identifier, can't be used as parameter name
                        // Change it to "ps_data"
                        case "DATA":
                            powerShell += "   -PSData " + parameter.Value + " `" + Environment.NewLine;
                            break;

                        // Respect parameter name from Ruby, handle it by resource PS function
                        default:
                            powerShell += "   -" + textInfo.ToTitleCase(parameter.Key) + " " + parameter.Value + " `" + Environment.NewLine;
                            break;
                    }
                }
                // Trim end of PowerShell string and add semicolon after the last line
                string end = " `" + Environment.NewLine;
                powerShell = powerShell.TrimEnd(end.ToCharArray()) + ";";
                powerShell += notification;
                powerShell += Environment.NewLine + Environment.NewLine;

                // Handle notification
                if (notifiers.ContainsKey(resource.Key))
                {
                    List<string> notifierArguments = notifiers[resourceUniqueName];
                    string notifiedTempBlock = String.Empty;
                    string notifiedUniqueName = notifierArguments[1];
                    string[] notifiedUniqueNameSplit = Regex.Split(notifierArguments[1], @"->");
                    string notifiedFunctionName = notifiedUniqueNameSplit[0];
                    string notifiedResourceNameParameter = notifiedUniqueNameSplit[1].SingleQuote();
                    Dictionary<string, string> notifiedParameters = GeneratedDictionary["resources"][notifiedUniqueName];
                    notifiedTempBlock += notifiedFunctionName;
                    notifiedTempBlock += " -ResourceName " + notifiedResourceNameParameter + " `" + Environment.NewLine;
                    foreach (KeyValuePair<string, string> parameter in notifiedParameters)
                    {
                        switch (parameter.Key.ToUpper())
                        {
                            // Notifies are not handeled here
                            case "NOTIFIES":
                                continue;

                            // "data" is a PowerShell reserved identifier, can't be used as parameter name
                            // Change it to "ps_data"
                            case "DATA":
                                notifiedTempBlock += "   -PSData " + parameter.Value + " `" + Environment.NewLine;
                                break;

                            // Respect parameter name from Ruby, handle it by resource PS function
                            default:
                                notifiedTempBlock += "   -" + textInfo.ToTitleCase(parameter.Key) + " " + parameter.Value + " `" + Environment.NewLine;
                                break;
                        }
                    }
                    // Trim end of notifiedTempBlock and add semicolon after the last line
                    notifiedTempBlock = notifiedTempBlock.TrimEnd(end.ToCharArray()) + ";";
                    if (notifierArguments[2].ToUpper() == "IMMEDIATELY")
                    {
                        powerShell += "#_____________Begin Immdetiate Notification_______________"
                                      + Environment.NewLine;
                        powerShell += notifiedTempBlock;
                        powerShell += Environment.NewLine
                                      + "#______________End Immdetiate Notification________________"
                                      + Environment.NewLine + Environment.NewLine + Environment.NewLine;
                    }
                    else
                    {
                        delayedNotifications += notifiedTempBlock + Environment.NewLine + Environment.NewLine;
                    }
                }
            }

            if (delayedNotifications != String.Empty)
            {
                powerShell += Environment.NewLine
                                + "#______________Begin Delayed Notifications________________"
                                + Environment.NewLine;

                powerShell += delayedNotifications;

                powerShell += "#________________End Delayed Notification_________________"
                                + Environment.NewLine;

            }
            return powerShell;
        }
    }
}
﻿/*
Ferram Aerospace Research v0.14.6
Copyright 2014, Michael Ferrara, aka Ferram4

    This file is part of Ferram Aerospace Research.

    Ferram Aerospace Research is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Ferram Aerospace Research is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Ferram Aerospace Research.  If not, see <http://www.gnu.org/licenses/>.

    Serious thanks:		a.g., for tons of bugfixes and code-refactorings
            			Taverius, for correcting a ton of incorrect values
            			sarbian, for refactoring code for working with MechJeb, and the Module Manager 1.5 updates
            			ialdabaoth (who is awesome), who originally created Module Manager
                        Regex, for adding RPM support
            			Duxwing, for copy editing the readme
 * 
 * Kerbal Engineer Redux created by Cybutek, Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License
 *      Referenced for starting point for fixing the "editor click-through-GUI" bug
 *
 * Part.cfg changes powered by sarbian & ialdabaoth's ModuleManager plugin; used with permission
 *	http://forum.kerbalspaceprogram.com/threads/55219
 *
 * Toolbar integration powered by blizzy78's Toolbar plugin; used with permission
 *	http://forum.kerbalspaceprogram.com/threads/60863
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FerramAerospaceResearch.FARGUI.FARFlightGUI
{
    class AirspeedSettingsGUI
    {
        Vessel _vessel;

        GUIStyle buttonStyle;

        public AirspeedSettingsGUI(Vessel vessel)
        {
            _vessel = vessel;
        }

        public enum SurfaceVelMode
        {
            TAS,
            IAS,
            EAS,
            MACH
        }

        private string[] surfModel_str = 
        {
            "Surface",
            "IAS",
            "EAS",
            "Mach"
        };

        public enum SurfaceVelUnit
        {
            M_S,
            KNOTS,
            MPH,
            KM_H,
        }

        private string[] surfUnit_str = 
        {
            "m/s",
            "knots",
            "mph",
            "km/h"
        };

        SurfaceVelMode velMode = SurfaceVelMode.TAS;
        SurfaceVelUnit unitMode = SurfaceVelUnit.M_S;

        private List<InternalSpeed> speedometers = null;

        public void AirSpeedSettings()
        {
            if (buttonStyle == null)
                buttonStyle = FlightGUI.buttonStyle;

            GUILayout.BeginVertical();
            GUILayout.Label("Select Surface Velocity Settings");
            GUILayout.Space(10);
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            velMode = (SurfaceVelMode)GUILayout.SelectionGrid((int)velMode, surfModel_str, 1, buttonStyle);
            unitMode = (SurfaceVelUnit)GUILayout.SelectionGrid((int)unitMode, surfUnit_str, 1, buttonStyle);
            GUILayout.EndHorizontal();

            //            SaveAirSpeedPos.x = AirSpeedPos.x;
            //            SaveAirSpeedPos.y = AirSpeedPos.y;
        }

        public void ChangeSurfVelocity()
        {
            if (FlightGlobals.ActiveVessel != _vessel)
            {
                if (speedometers != null)
                    speedometers = null;
                return;
            }
            //DaMichel: Keep our fingers off of this also if there is no atmosphere (staticPressure <= 0)
            if (FlightUIController.speedDisplayMode != FlightUIController.SpeedDisplayModes.Surface || _vessel.atmDensity <= 0)
                return;
            FlightUIController UI = FlightUIController.fetch;

            if (UI.spdCaption == null || UI.speed == null)
                return;

            string speedometerCaption = "Surf: ";
            double unitConversion = 1;
            string unitString = "m/s";
            if (unitMode == SurfaceVelUnit.KNOTS)
            {
                unitConversion = 1.943844492440604768413343347219;
                unitString = "knots";
            }
            else if (unitMode == SurfaceVelUnit.KM_H)
            {
                unitConversion = 3.6;
                unitString = "km/h";
            }
            else if (unitMode == SurfaceVelUnit.MPH)
            {
                unitConversion = 2.236936;
                unitString = "mph";
            }
            if (velMode == SurfaceVelMode.TAS)
            {
                UI.spdCaption.text = "Surface";
                UI.speed.text = (_vessel.srfSpeed * unitConversion).ToString("F1") + unitString;
            }
            else
            {
                if (velMode == SurfaceVelMode.IAS)
                {
                    UI.spdCaption.text = "IAS";
                    speedometerCaption = "IAS: ";
                    double densityRatio = (FARAeroUtil.GetCurrentDensity(_vessel.mainBody, _vessel.altitude, false) * 1.225);
                    double pressureRatio = FARAeroUtil.StagnationPressureCalc(_vessel.mach);
                    UI.speed.text = (_vessel.srfSpeed * Math.Sqrt(densityRatio) * pressureRatio * unitConversion).ToString("F1") + unitString;
                }
                else if (velMode == SurfaceVelMode.EAS)
                {
                    UI.spdCaption.text = "EAS";
                    speedometerCaption = "EAS: ";
                    double densityRatio = (FARAeroUtil.GetCurrentDensity(_vessel.mainBody, _vessel.altitude, false) * 1.225);
                    UI.speed.text = (_vessel.srfSpeed * Math.Sqrt(densityRatio) * unitConversion).ToString("F1") + unitString;
                }
                else// if (velMode == SurfaceVelMode.MACH)
                {
                    UI.spdCaption.text = "Mach";
                    speedometerCaption = "Mach: ";
                    UI.speed.text = _vessel.mach.ToString("F3");
                }
            }
            /* DaMichel: cache references to current IVA speedometers.
             * IVA stuff is reallocated whenever you switch between vessels. So i see
             * little point in storing the list of speedometers permanently. It just has
             * to be freshly cached whenever something changes. */
            if (FlightGlobals.ready)
            {
                if (speedometers == null)
                {
                    speedometers = new List<InternalSpeed>();
                    for (int i = 0; i < _vessel.parts.Count; ++i)
                    {
                        Part p = _vessel.parts[i];
                        if (p && p.internalModel)
                        {
                            speedometers.AddRange(p.internalModel.GetComponentsInChildren<InternalSpeed>());
                        }
                    }
                    //Debug.Log("FAR: Got new references to speedometers"); // check if it is really only executed when vessel change
                }
                string text = speedometerCaption + UI.speed.text;
                for (int i = 0; i < speedometers.Count; ++i)
                {
                    speedometers[i].textObject.text.Text = text; // replace with FAR velocity readout
                }
            }
        }

        void ResetSpeedometers()
        {

        }
    }
}
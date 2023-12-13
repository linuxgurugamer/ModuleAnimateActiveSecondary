using System;
using System.Collections.Generic;
using System.Linq;
using KSP_Log;

using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
using System.Diagnostics;
using System.Collections;
using static ModuleAnimateGeneric;

namespace ModuleAnimateGauge
{
    public class ModuleAnimateGauge : PartModule
    {
        [KSPField]
        public string animationName = null;

        [KSPField]
        public string gaugeResource = null;

        [KSPField]
        public bool requireDeploy = false;

        [KSPField]
        public bool debug = false;


        Animation animation = null;
        int resID;
        bool ramped = false;

        internal static Log Log = null;



        internal void InitLog()
        {
#if DEBUG
            Log = new Log("ModuleAnimateGauge", Log.LEVEL.INFO);
#else
            Log = new Log("ModuleAnimateGauge.", Log.LEVEL.ERROR);
#endif
        }

        void DumpConfig()
        {
            if (debug)
            {
                Log.Info("animationName: " + animationName);
                Log.Info("gaugeResource: " + gaugeResource);
                Log.Info("requireDeploy: " + requireDeploy);
                Log.Info("resId: " + resID);
            }
        }

        ModuleAnimationGroup mag = null;

        Animation[] ma;
        void Start()
        {
            InitLog();

            if (animationName != null)
            {
                ma = part.FindModelAnimators(animationName);
                if (ma != null && ma.Length > 0)
                    animation = ma[0];
                else
                    Log.Error("Animation: " + animationName + " not found");

                PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(gaugeResource);
                if (resource != null)
                    resID = resource.id;
                else
                {
                    Log.Error("Resource: " + gaugeResource + " not found");
                    return;
                }

                if (requireDeploy)
                {
                    mag = part.FindModuleImplementing<ModuleAnimationGroup>();
                    if (mag == null)
                        Log.Info("mag is null");
                }
                if (animation != null)
                {
                    animation[animationName].speed = 0;
                    animation[animationName].enabled = true;
                }
                else
                {
                    Log.Error("animation == null");
                    return;
                }
                DumpConfig();

#if DEBUG
                StartCoroutine(SlowUpdate());
#endif
            }
        }

#if DEBUG
        IEnumerator SlowUpdate()
        {
            while (true)
            {
#else
                public void LateUpdate()
        {
#endif
                if (!requireDeploy || (mag != null && mag.isDeployed))
                {
                    part.GetConnectedResourceTotals(resID, out double resAmount, out double maxAmount);
                    double percent = resAmount / maxAmount;
                    // Ramping assumes that the animation starts at 0
                    // and is only done when the mod first starts running
                    if (!ramped)
                    {
                        Log.Info("!ramped");
                        if (percent > 0)
                        {
                            if (animation[animationName].normalizedTime >= percent)
                            {
                                Log.Info("animation[animationName].normalizedTime >= percent");
                                ramped = true;
                                animation[animationName].speed = 0;
                            }
                            else
                            {
                                Log.Info("Setting speed = 1");
                                animation[animationName].speed = 1f;
                                animation.Play();
                            }
                        }
                        else
                        {
                            Log.Info("percent == 0");
                            ramped = true;
                            animation[animationName].speed = 0;
                        }
                    }
                    else
                    {
                        Log.Info("Setting normalizedTie = " + percent);
                        animation[animationName].normalizedTime = (float)percent;
                        animation.Play();
                    }
                }
#if DEBUG
                yield return new WaitForSeconds(0.05f);
            }
#endif
        }
    }
}

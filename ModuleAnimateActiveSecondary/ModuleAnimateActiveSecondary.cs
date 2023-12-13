using KSP_Log;
using System.Linq;
using System.Collections;

using UnityEngine;
using System.Diagnostics.Eventing.Reader;

namespace ModuleAnimateActiveSecondary
{
    public class ModuleAnimateActiveSecondary : PartModule
    {
        [KSPField]
        public string animationName = "";

        [KSPField]
        public bool requireDeploy = false;

        [KSPField]
        public string requireResource = null;

        [KSPField]
        public string flameoutResource = null;

        [KSPField]
        public string flameoutHideTransform = null;

        [KSPField]
        public bool debug = false;


        internal static Log Log = null;

        internal  int? resID = null;
        internal  int? flameoutResID = null;
        double lastResAmount = 0, lastFlameoutResAmount = 0;
        bool flameoutAnimationVisible = true;

        protected Animation anim;
        Transform[] flameOutHideTransforms { get; set; }

        ModuleAnimationGroup mag = null;


        internal void InitLog()
        {
#if DEBUG
            Log = new Log("ModuleAnimateActiveSecondary", Log.LEVEL.INFO);
            debug = true;
#else
            Log = new Log("ModuleAnimateActiveSecondary.", Log.LEVEL.ERROR);
#endif
        }

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            InitLog();
            if (debug)
                Log.SetLevel(Log.LEVEL.INFO);
            else
                Log.SetLevel(Log.LEVEL.ERROR);

            Log.Info("Start, part: " + part.name);
            if (animationName != "")
            {
                anim = part.FindModelAnimators(animationName).FirstOrDefault();
                if ((object)anim == null)
                {
                    Log.Error("Start(): Animation not found: " + animationName);
                }
                else
                {
                    Log.Info("Start() - Animation found named " + animationName);
                }
            }
            else
            {
                Log.Info("No animation specified");
                return;
            }
            if (flameoutHideTransform != null)
                flameOutHideTransforms = part.FindModelTransforms(flameoutHideTransform);

            if (requireDeploy)
                mag = part.FindModuleImplementing<ModuleAnimationGroup>();

            if (requireResource != null && requireResource != "")
            {
                PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(requireResource);
                if (resource != null)
                    resID = resource.id;
                else
                    Log.Error("Resource: " + requireResource + " not found");
            }

            if (flameoutResource == null || flameoutResource == "")
                flameoutResource = requireResource;
            if (flameoutResource != null)
            {
                PartResourceDefinition flameoutResPRD = PartResourceLibrary.Instance.GetDefinition(flameoutResource);
                if (flameoutResPRD != null)
                    flameoutResID = flameoutResPRD.id;
                else
                    Log.Error("Flameout Resource: " + flameoutResource + " not found");
            }

#if DEBUG
            DumpConfig();
#endif

            ///
            // Work on the Resource here
            ///
            if (resID != null)
                part.GetConnectedResourceTotals((int)resID, out lastResAmount, out double maxAmount);
            else
                lastResAmount = 1;
            if (lastResAmount >= 0.001f)
            {
                if ((mag != null && mag.isDeployed) || !requireDeploy)
                    StartAnimation();
            }

            ///
            // Work on the Flameout Resource here
            //
            if (flameoutResID != null)
            {
                part.GetConnectedResourceTotals((int)flameoutResID, out lastFlameoutResAmount, out double maxAmount);
                {
                    if (lastFlameoutResAmount >= 0.001f)
                    {
                        ShowFlameoutTransforms();
                    }
                    else
                    {
                        HideFlameoutTransforms();
                    }
                }
            }
            StartCoroutine(SlowUpdate());

        }

        IEnumerator SlowUpdate()
        {
            while (true)
            {
                ///
                // First get the amount of the resources
                ///
                double resAmount = 1, flameoutResAmt = 1;

                // Probably need to change the following from "vessel" to "part"
                if (resID != null)
                    part.GetConnectedResourceTotals((int)resID, out resAmount, out double maxAmount);
                if (flameoutResID != null)
                    part.GetConnectedResourceTotals((int)flameoutResID, out flameoutResAmt, out double maxAmount);

                ///
                // Work on the Flameout Resource here
                //

                if (flameoutResID != null)
                {
                    if (FlameoutTransformsVisible(resAmount, flameoutResID, flameoutResAmt))
                    {
                        ShowFlameoutTransforms();

                        ///
                        // Work on the Resource here
                        ///

                        if (AnimationActive(resAmount, resID, resAmount))
                            StartAnimation();
                        else
                            StopAnimation();
                    }
                    else
                    {
                        HideFlameoutTransforms();
                        StopAnimation();
                    }
                } else
                {
                    if (AnimationActive(resAmount, resID, resAmount))
                        StartAnimation();
                    else
                        StopAnimation();

                }

                ///
                // Finally, save the last amounts and sleep for a second
                //
                lastResAmount = resAmount;
                lastFlameoutResAmount = flameoutResAmt;
                yield return new WaitForSeconds(1f);
            }
        }

        /// 
        /// The logic for whether the animation is active and the FlameoutTransforms are visible
        /// is controlled by the following two methods
        /// 
        bool AnimationActive(double resAmount, int? flameoutResId, double flameoutResAmt)
        {
            bool rc;
            if (resAmount < 0.001f)
            {
                rc = false;
            }
            else
            {
                if ((mag != null && mag.isDeployed) || !requireDeploy)
                {
                    if (flameoutResID == null || flameoutResAmt > 0.001f)
                        rc = true;
                    else
                        rc = false;
                }
                else
                    rc = false;
            }
            Log.Info("AnimationActive, resAmount: " + resAmount + ", rc: " + rc);
            return rc;
        }

        bool FlameoutTransformsVisible(double resAmount, int? flameoutResId, double flameoutResAmt)
        {
            bool rc;
            if (flameoutResAmt < 0.001f)
            {
                rc = false;
            }
            else
            {
                if ((mag != null && mag.isDeployed) || !requireDeploy)
                    rc = true;
                else
                    rc = false;
            }
            if (flameOutHideTransforms != null && flameOutHideTransforms.Length > 0)
            {
                if (resAmount < 0.001f)
                    rc = false;
            }
            Log.Info("FlameoutTransformsVisible, flameoutResAmt: " + flameoutResAmt + ", rc: " + rc);
            return rc;
        }


        void StopAnimation()
        {
            if (anim.IsPlaying(animationName))
            {
                if (flameOutHideTransforms == null)
                    Log.Info("StopAnimation, no flameOutHideTransforms");
                else
                    Log.Info("StopAnimation, sizeof flameOutHideTransforms: " + flameOutHideTransforms.Length);
                anim.Stop(animationName);
            }
        }

        void HideFlameoutTransforms()
        {
            Log.Info("HideFlameoutTransforms, part: " + part.name + ", flameoutAnimationVisible: " + flameoutAnimationVisible);
            if (flameoutHideTransform != null)
                Log.Info("part: " + part.name + ", flameoutHideTransform " + flameoutHideTransform +
                    ", flameoutResource: " + flameoutResource);
            if (flameOutHideTransforms != null && flameoutAnimationVisible)
            {
                Log.Info("StopFlameoutAnimation");
                foreach (Transform f in flameOutHideTransforms)
                    f.gameObject.SetActive(false);
                flameoutAnimationVisible = false;
            }
        }

        void StartAnimation()
        {
            if (!anim.IsPlaying(animationName))
            {
                if (flameOutHideTransforms == null)
                    Log.Info("StartAnimation, no flameOutHideTransforms");
                else
                    Log.Info("StartAnimation, sizeof flameOutHideTransforms: " + flameOutHideTransforms.Length);
                anim.Play(animationName);
            }
        }

        void ShowFlameoutTransforms()
        {
            Log.Info("ShowFlameoutTransforms, flameoutAnimationVisible: " + flameoutAnimationVisible);
            if (flameOutHideTransforms != null && !flameoutAnimationVisible)
            {
                Log.Info("StartFlameoutAnimation, sizeof flameOutHideTransforms: " + flameOutHideTransforms.Length);
                //anim.Play(animationName);
                foreach (Transform f in flameOutHideTransforms)
                    f.gameObject.SetActive(true);
                flameoutAnimationVisible = true;
            }
        }

#if DEBUG
        internal void DumpConfig()
        {
            if (this.part != null)
            {
                if (this.part.name != null)
                    Log.Info("DumpConfig, part: " + this.part.name);
                if (this.part.partInfo != null)
                    Log.Info("DumpConfig, part.partInfo.title: " + this.part.partInfo.title);
            }
            Log.Info("DumpConfig, animationName: " + animationName);
            Log.Info("DumpConfig, requireDeploy: " + requireDeploy);
            Log.Info("DumpConfig, requireResource: " + requireResource);
            Log.Info("DumpConfig, flameoutResource: " + flameoutResource);
            Log.Info("DumpConfig, flameoutResID: " + flameoutResID);
            if (flameoutHideTransform != null)
            {
                Log.Info("DumpConfig, flameoutHideTransform: " + flameoutHideTransform + ", # transforms found: " + flameOutHideTransforms.Length);
                foreach (var f in flameOutHideTransforms)
                {
                    Log.Info("flameOutHideTransform: " + f);
                }
            }

            Log.Info("DumpConfig, animation info:");
            Log.Info("anim.IsActiveAndEnabled: " + anim.isActiveAndEnabled);
            Log.Info("anim.clip: " + anim.clip);
            Log.Info("anim.playAutomatically: " + anim.playAutomatically);
            Log.Info("anim.wrapMode: " + anim.wrapMode);
            Log.Info("anim.isPlaying: " + anim.isPlaying);
        }
#endif


    }
}

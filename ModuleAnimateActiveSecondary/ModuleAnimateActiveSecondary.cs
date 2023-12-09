using KSP_Log;
using System.Linq;
using System.Collections;

using UnityEngine;



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
        internal static int? resID = null;
        internal static int? flameoutResID = null;
        double lastResAmount = 0, lastFlameoutResAmount = 0;
        protected Animation anim;
        public Transform[] flameOutHideTransforms { get; private set; }

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
            flameOutHideTransforms = part.FindModelTransforms(flameoutHideTransform);

            if (requireDeploy)
                mag = part.FindModuleImplementing<ModuleAnimationGroup>();

            if (requireResource != null)
            {
                PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(requireResource);
                if (resource != null)
                    resID = resource.id;
                else
                    Log.Error("Resource: " + requireResource + " not found");
            }

            if (flameoutResource == null)
                flameoutResource = requireResource;
            if (flameoutResource != null)
            {
                PartResourceDefinition flameoutResPRD = PartResourceLibrary.Instance.GetDefinition(flameoutResource);
                if (flameoutResPRD != null)
                    flameoutResID = flameoutResPRD.id;
                else
                    Log.Error("Resource: " + flameoutResource + " not found");
            }

#if DEBUG
            DumpConfig();
#endif

            ///
            // Work on the Resource here
            ///
            if (resID != null)
                vessel.GetConnectedResourceTotals((int)resID, out lastResAmount, out double maxAmount);
            else
                lastResAmount = 1;
            if (lastResAmount >= 0.001f)
            {
                if (mag == null || mag.isDeployed || !requireDeploy)
                    StartAnimation();
            }

            ///
            // Work on the Flameout Resource here
            //
            if (flameoutResID != null)
                vessel.GetConnectedResourceTotals((int)flameoutResID, out lastFlameoutResAmount, out double maxAmount);
            else
                lastFlameoutResAmount = 1;
            if (lastFlameoutResAmount >= 0.001f)
            {
                ShowFlameoutAnimation();
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
                if (resID != null)
                    vessel.GetConnectedResourceTotals((int)resID, out resAmount, out double maxAmount);
                if (flameoutResID != null)
                    vessel.GetConnectedResourceTotals((int)flameoutResID, out flameoutResAmt, out double maxAmount);

                ///
                // Work on the Resource here
                ///

                if (resAmount < 0.001f)
                {
                    if (lastResAmount >= 0.001f)
                    {
                        StopAnimation();
                    }
                }
                else
                {
                    if (lastResAmount < 0.001f)
                    {
                        if ((mag != null && mag.isDeployed) || !requireDeploy)
                        {
                            StartAnimation();
                        }
                    }
                    else
                    {
                        if (mag != null || !requireDeploy)
                        {
                            if (requireDeploy && !mag.isDeployed)
                            {
                                StopAnimation();
                            }
                            else
                            {
                                if (!anim.IsPlaying(animationName))
                                {
                                    StartAnimation();
                                }
                            }
                        }
                    }
                }

                ///
                // Work on the Flameout Resource here
                //

                if (flameoutResAmt < 0.001f)
                {
                    if (lastFlameoutResAmount >= 0.001f)
                        HideFlameoutAnimation();
                }
                else
                {
                    if (lastFlameoutResAmount < 0.001f)
                    {
                        if ((mag != null && mag.isDeployed) || !requireDeploy)
                        {
                            ShowFlameoutAnimation();
                        }
                    }
                    else
                    {
                        if (mag != null || !requireDeploy)
                        {
                            if (requireDeploy && !mag.isDeployed)
                            {
                                HideFlameoutAnimation();
                            }
                            else
                            {
                                ShowFlameoutAnimation();
                            }
                        }
                    }
                }

                ///
                // Finally, save the last amounts and sleep for a second
                //
                lastResAmount = resAmount;
                lastFlameoutResAmount = flameoutResAmt;
                yield return new WaitForSeconds(1f);
            }
        }

        void StopAnimation()
        {
            Log.Info("StopAnimation, sizeof flameOutHideTransforms: " + flameOutHideTransforms.Length);
            anim.Stop(animationName);
        }

        void HideFlameoutAnimation()
        {
            Log.Info("StopFlameoutAnimation");
            foreach (Transform f in flameOutHideTransforms)
                f.gameObject.SetActive(false);
        }

        void StartAnimation()
        {
            Log.Info("StartAnimation, sizeof flameOutHideTransforms: " + flameOutHideTransforms.Length);
            anim.Play(animationName);
        }
        void ShowFlameoutAnimation()
        {
            Log.Info("StartFlameoutAnimation, sizeof flameOutHideTransforms: " + flameOutHideTransforms.Length);
            //anim.Play(animationName);
            foreach (Transform f in flameOutHideTransforms)
                f.gameObject.SetActive(true);
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

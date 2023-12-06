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
        public string flameoutHideTransform = null;

        [KSPField]
        public bool debug = false;


        internal static Log Log = null;
        internal static int resID = -1;
        double lastECamount = 0;
        protected Animation anim;
        public Transform[] flameOutHideTransforms { get; private set; }

        ModuleAnimationGroup mag = null;


        internal void InitLog()
        {
#if DEBUG
            Log = new Log("ModuleAnimateActiveSecondary", Log.LEVEL.INFO);
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
                    Log.Error("ModuleAnimateActiveSecondary: Animation not found: " + animationName);
                }
                else
                {
                    Log.Info("ModuleAnimateActiveSecondary.Start() - Animation found named " + animationName);
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

#if DEBUG
            DumpConfig();
#endif
            if (requireResource != null)
            {
                PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(requireResource);
                if (resource != null)
                    resID = resource.id;
                else
                    Log.Error("Resource: " + requireResource + " not found");
            }
            if (resID > 0)
                vessel.GetConnectedResourceTotals(resID, out lastECamount, out double maxAmount);
            else
                lastECamount = 1;
            if (lastECamount >= 0.001f)
            {
                if (mag == null || mag.isDeployed)
                    StartAnimation();
            }
            StartCoroutine(SlowUpdate());

        }
        IEnumerator SlowUpdate()
        {
            while (true)
            {
                double amount = 1;
                if (resID > 0)
                    vessel.GetConnectedResourceTotals(resID, out amount, out double maxAmount);
                if (amount < 0.001f)
                {
                    if (lastECamount >= 0.001f)
                    {
                        StopAnimation();
                    }
                }
                else
                {
                    if (lastECamount < 0.001f)
                    {
                        if (mag != null && mag.isDeployed)
                        {
                            StartAnimation();
                        }
                    }
                    else
                    {
                        if (mag != null)
                        {
                            if (!mag.isDeployed)
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
                lastECamount = amount;
                yield return new WaitForSeconds(1f);
            }
        }

        void StopAnimation()
        {
            anim.Stop(animationName);
            foreach (Transform f in flameOutHideTransforms)
                f.gameObject.SetActive(false);
        }

        void StartAnimation()
        {
            anim.Play(animationName);
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

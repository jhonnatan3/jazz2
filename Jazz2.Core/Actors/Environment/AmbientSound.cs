﻿using Duality.Audio;
using static Duality.Component;

namespace Jazz2.Actors.Environment
{
    // https://www.jazz2online.com/jcf/showthread.php?t=13205

    public class AmbientSound : ActorBase
    {
        private SoundInstance sound;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            // ToDo: Implement Fade:1|Sine:1

            collisionFlags = CollisionFlags.None;

            RequestMetadata("Common/AmbientSound");

            string name;
            switch (details.Params[0])
            {
                case 0: name = "AmbientWind"; break;
                case 1: name = "AmbientFire"; break;
                case 2: name = "AmbientScienceNoise"; break;
                default: return;
            }

            float gain = (details.Params[1] / 255f);

            sound = PlaySound(name, gain);
            sound.Looped = true;
            sound.BeginFadeIn(1f);
        }

        protected override void OnUpdate()
        {
            // Nothing to do...
        }

        protected override void OnDeactivated(ShutdownContext context)
        {
            if (sound != null) {
                sound.FadeOut(0.8f);
                sound = null;
            }
        }
    }
}
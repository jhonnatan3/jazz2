﻿namespace Jazz2.Actors.Collectibles
{
    public class Stopwatch : Collectible
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            SetAnimation("Stopwatch");

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            if (player.IncreaseShieldTime(10f)) {
                base.Collect(player);
            }
        }
    }
}
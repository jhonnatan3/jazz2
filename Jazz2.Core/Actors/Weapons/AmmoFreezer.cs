﻿using Duality;
using Duality.Resources;
using Jazz2.Game;
using Jazz2.Game.Structs;
using static Jazz2.Game.Tiles.TileMap;

namespace Jazz2.Actors.Weapons
{
    public class AmmoFreezer : AmmoBase
    {
        public override WeaponType WeaponType => WeaponType.Freezer;

        public float FrozenDuration => ((upgrades & 0x1) != 0 ? 280f : 180f);

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;
            strength = 0;

            RequestMetadata("Weapon/Freezer");

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.8f;
            light.Brightness = 0.2f;
            light.RadiusNear = 0f;
            light.RadiusFar = 20f;
        }

        public void OnFire(Player owner, Vector3 speed, float angle, bool isFacingLeft, byte upgrades)
        {
            base.owner = owner;
            base.IsFacingLeft = isFacingLeft;
            base.upgrades = upgrades;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            float baseSpeed = ((upgrades & 0x1) != 0 ? 8f : 6f);
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;
            speedY += MathF.Abs(speed.Y) * speedY;

            AnimState state = AnimState.Idle;

            if ((upgrades & 0x1) != 0) {
                timeLeft = 38;
                state |= (AnimState)1;
                PlaySound("FireUpgraded");
            } else {
                timeLeft = 44;
                
                PlaySound("Fire");
            }

            Transform.Angle = angle;

            SetAnimation(state);
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();
            CheckCollisions();
            TryStandardMovement(Time.TimeMult);

            base.OnUpdate();

            Material material = currentAnimation.Material.Res;
            Texture texture = material.MainTexture.Res;

            Vector3 pos = Transform.Pos;
            float dx = MathF.Rnd.NextFloat(-8f, 8f);
            float dy = MathF.Rnd.NextFloat(-3f, 3f);

            const float currentSize = 1f;
            int currentFrame = renderer.CurrentFrame;

            api.TileMap.CreateDebris(new DestructibleDebris {
                Pos = new Vector3(pos.X + dx, pos.Y + dy, pos.Z + 1f),
                Size = new Vector2(currentSize, currentSize),
                Acceleration = new Vector2(0f, api.Gravity),

                Scale = 1.2f,
                Alpha = 1f,

                Time = 300f,

                Material = material,
                MaterialOffset = new Rect(
                    (((float)(currentFrame % currentAnimation.Base.FrameConfiguration.X) / currentAnimation.Base.FrameConfiguration.X) + ((float)dx / texture.ContentWidth) + 0.5f) * texture.UVRatio.X,
                    (((float)(currentFrame / currentAnimation.Base.FrameConfiguration.X) / currentAnimation.Base.FrameConfiguration.Y) + ((float)dy / texture.ContentHeight) + 0.5f) * texture.UVRatio.Y,
                    (currentSize * texture.UVRatio.X / texture.ContentWidth),
                    (currentSize * texture.UVRatio.Y / texture.ContentHeight)
                ),

                CollisionAction = DebrisCollisionAction.Disappear
            });

            if (timeLeft <= 0f) {
                PlaySound("WallPoof");
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Explosion.Create(api, Transform.Pos + Speed, Explosion.SmokeWhite);

            return base.OnPerish(collider);
        }

        protected override void OnHitFloorHook()
        {
            DecreaseHealth(int.MaxValue);

            PlaySound("WallPoof");
        }

        protected override void OnHitWallHook()
        {
            DecreaseHealth(int.MaxValue);

            PlaySound("WallPoof");
        }

        protected override void OnHitCeilingHook()
        {
            DecreaseHealth(int.MaxValue);

            PlaySound("WallPoof");
        }
    }
}
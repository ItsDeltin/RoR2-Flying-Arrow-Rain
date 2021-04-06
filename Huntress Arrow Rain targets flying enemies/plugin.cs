using System.Linq;
using System.Reflection;
using UnityEngine;
using BepInEx;
using OnArrowRain = On.EntityStates.Huntress.ArrowRain;
using ArrowRain = EntityStates.Huntress.ArrowRain;
using BaseState = EntityStates.BaseState;
using LayerIndex = RoR2.LayerIndex;
using HurtBox = RoR2.HurtBox;

namespace Deltin
{
    [BepInPlugin("com.deltin.huntressarrowraintargetsflyingenemies", "Huntress Arrow Rain targets flying enemies", "1.0.0")]
    public class Huntress_Arrow_Rain_targets_flying_enemies : BaseUnityPlugin
    {
        public void Awake()
        {
            // Hook ArrowRain.UpdateAreaIndicator which determines the position of the arrow rain zone.
            // This completely overrides hopoo's implementation, so any changes they make to UpdateAreaIndicator will need to be applied here.
            OnArrowRain.UpdateAreaIndicator += (orig, self) => {
                // 'fields' exposes private elements in the ArrowRain class that we wouldn't be able to access normally.
                var fields = new ArrowRainFields(self);

                // Ensure that the area indicator is active.
                if (fields.AreaIndicatorInstance)
                {
                    // Ray has the position and direction of the raycast which is shot out from the player's aim.
                    Ray ray = fields.GetAimRay();

                    // Get the world raycast.
                    bool wasWorldHit = Physics.Raycast(ray.origin, ray.direction, out RaycastHit worldHit, 1000f, LayerIndex.world.mask);

                    // Get the entity raycast.
                    var entityHits = Physics.RaycastAll(ray, 1000f, LayerIndex.entityPrecise.mask).OrderBy(hit => hit.distance);

                    Vector3 point = default(Vector3);
                    Vector3 normal = default(Vector3);
                    bool set = false;

                    // Check entity hits.
                    foreach (var entityHit in entityHits)
                    {
                        // Get the hurtbox from the same gameobject the collider is linked to.
                        var hurtbox = entityHit.collider.GetComponent<HurtBox>();

                        // Make sure the enemy is a flying enemy.
                        if (hurtbox?.healthComponent?.body && hurtbox.healthComponent.body.isFlying)
                        {
                            // Set the point to the center of the hitbox + the ray direction.
                            // Adding the point to the ray direction ensures that the arrows will actually reach the target.
                            point = entityHit.collider.bounds.center + ray.direction * 2;
                            normal = entityHit.normal;
                            set = true;
                            break;
                        }
                    }

                    // If no entities were hit, set the point and normal to the world hit.
                    if (!set && wasWorldHit)
                    {
                        point = worldHit.point;
                        normal = worldHit.normal;
                        set = true;
                    }

                    // Set the area indicator to the specified point and normal.
                    if (set)
                    {
                        fields.AreaIndicatorInstance.transform.position = point;
                        fields.AreaIndicatorInstance.transform.up = normal;
                    }
                }
            };
        }
    }

    // Exposes private ArrowRain fields.
    class ArrowRainFields
    {
        public GameObject AreaIndicatorInstance {
            get => (GameObject)areaIndicatorInstance.GetValue(arrowRain);
            set => areaIndicatorInstance.SetValue(arrowRain, value);
        }

        public Ray GetAimRay() => (Ray)getAimRay.Invoke(arrowRain, new object[0]);

        readonly ArrowRain arrowRain;
        readonly FieldInfo areaIndicatorInstance;
        readonly MethodInfo getAimRay;

        public ArrowRainFields(ArrowRain arrowRain)
        {
            this.arrowRain = arrowRain;
            // Fields
            this.areaIndicatorInstance = typeof(ArrowRain).GetField("areaIndicatorInstance", BindingFlags.NonPublic | BindingFlags.Instance);
            // Functions
            this.getAimRay = typeof(BaseState).GetMethod("GetAimRay", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
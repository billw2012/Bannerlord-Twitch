#if false
// Unused, has errors due to version changes in game
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;

namespace BannerlordTwitch.Util
{
    public class RenderExport
    {
        public GameEntity CreateItemBaseEntity(ItemObject item, Scene scene, ref Camera camera)
        {
            MatrixFrame identity = MatrixFrame.Identity;
            MatrixFrame identity2 = MatrixFrame.Identity;
            MatrixFrame identity3 = MatrixFrame.Identity;
            camera = Camera.CreateCamera();
            //GetItemPoseAndCamera(item, scene, ref camera, ref identity, ref identity2, ref identity3);
            return AddItem(scene, item, identity, identity2, identity3);
        }
	    
        private void GetItemPoseAndCamera(ItemObject item, Scene scene, ref Camera camera, 
            ref MatrixFrame itemFrame, ref MatrixFrame itemFrame1, ref MatrixFrame itemFrame2)
        {
            if (item.IsCraftedWeapon)
            {
                GetItemPoseAndCameraForCraftedItem(item, scene, ref camera, ref itemFrame, ref itemFrame1, ref itemFrame2);
                return;
            }
            string str = "";
            bool flag = false;
            if (item.WeaponComponent != null)
            {
                WeaponClass weaponClass = item.WeaponComponent.PrimaryWeapon.WeaponClass;
                if (weaponClass - WeaponClass.OneHandedSword <= 1)
                {
                    str = "sword";
                    flag = true;
                }
            }
            else
            {
                ItemObject.ItemTypeEnum type = item.Type;
                if (type != ItemObject.ItemTypeEnum.HeadArmor)
                {
                    if (type == ItemObject.ItemTypeEnum.BodyArmor)
                    {
                        str = "armor";
                        flag = true;
                    }
                }
                else
                {
                    str = "helmet";
                    flag = true;
                }
            }
            if (item.Type == ItemObject.ItemTypeEnum.Shield)
            {
                str = "shield";
                flag = true;
            }
            if (item.Type == ItemObject.ItemTypeEnum.Crossbow)
            {
                str = "crossbow";
                flag = true;
            }
            if (item.Type == ItemObject.ItemTypeEnum.Bow)
            {
                str = "bow";
                flag = true;
            }
            if (item.Type == ItemObject.ItemTypeEnum.LegArmor)
            {
                str = "boot";
                flag = true;
            }
            if (item.Type == ItemObject.ItemTypeEnum.Horse || item.Type == ItemObject.ItemTypeEnum.HorseHarness)
            {
                str = "horse";
                flag = true;
            }
            if (item.Type == ItemObject.ItemTypeEnum.Cape)
            {
                str = "cape";
                flag = true;
            }
            if (item.Type == ItemObject.ItemTypeEnum.HandArmor)
            {
                str = "glove";
                flag = true;
            }
            if (item.Type == ItemObject.ItemTypeEnum.Arrows)
            {
                str = "arrow";
                flag = true;
            }
            if (item.Type == ItemObject.ItemTypeEnum.Bolts)
            {
                str = "bolt";
                flag = true;
            }
            Game game = Game.Current;
            if (((game != null) ? game.DefaultItems : null) != null && (item == DefaultItems.IronOre || item == DefaultItems.HardWood || item == DefaultItems.Charcoal || item == DefaultItems.IronIngot1 || item == DefaultItems.IronIngot2 || item == DefaultItems.IronIngot3 || item == DefaultItems.IronIngot4 || item == DefaultItems.IronIngot5 || item == DefaultItems.IronIngot6 || item.ItemCategory == DefaultItemCategories.Silver))
            {
                str = "craftmat";
                flag = true;
            }
            string tag = str + "_cam";
            string tag2 = str + "_frame";
            if (flag)
            {
                GameEntity gameEntity = scene.FindEntityWithTag(tag);
                if (gameEntity != null)
                {
                    camera = Camera.CreateCamera();
                    Vec3 vec = default(Vec3);
                    gameEntity.GetCameraParamsFromCameraScript(camera, ref vec);
                }
                GameEntity gameEntity2 = scene.FindEntityWithTag(tag2);
                if (gameEntity2 != null)
                {
                    itemFrame = gameEntity2.GetGlobalFrame();
                    gameEntity2.SetVisibilityExcludeParents(false);
                }
            }
            else
            {
                GameEntity gameEntity3 = scene.FindEntityWithTag("goods_cam");
                if (gameEntity3 != null)
                {
                    camera = Camera.CreateCamera();
                    Vec3 vec2 = default(Vec3);
                    gameEntity3.GetCameraParamsFromCameraScript(camera, ref vec2);
                }
                GameEntity gameEntity4 = scene.FindEntityWithTag("goods_frame");
                if (gameEntity4 != null)
                {
                    itemFrame = gameEntity4.GetGlobalFrame();
                    gameEntity4.SetVisibilityExcludeParents(false);
                    gameEntity4.UpdateGlobalBounds();
                    MatrixFrame globalFrame = gameEntity4.GetGlobalFrame();
                    MetaMesh itemMeshForInventory = new ItemRosterElement(item, 0, null).GetItemMeshForInventory(false);
                    Vec3 vec3 = new Vec3(1000000f, 1000000f, 1000000f, -1f);
                    Vec3 vec4 = new Vec3(-1000000f, -1000000f, -1000000f, -1f);
                    if (itemMeshForInventory != null)
                    {
                        MatrixFrame identity = MatrixFrame.Identity;
                        for (int num = 0; num != itemMeshForInventory.MeshCount; num++)
                        {
                            Vec3 boundingBoxMin = itemMeshForInventory.GetMeshAtIndex(num).GetBoundingBoxMin();
                            Vec3 boundingBoxMax = itemMeshForInventory.GetMeshAtIndex(num).GetBoundingBoxMax();
                            Vec3[] array = new Vec3[]
                            {
                                globalFrame.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMin.z, -1f)),
                                globalFrame.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMax.z, -1f)),
                                globalFrame.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMin.z, -1f)),
                                globalFrame.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMax.z, -1f)),
                                globalFrame.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMin.z, -1f)),
                                globalFrame.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMax.z, -1f)),
                                globalFrame.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMin.z, -1f)),
                                globalFrame.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMax.z, -1f))
                            };
                            for (int i = 0; i < 8; i++)
                            {
                                vec3 = Vec3.Vec3Min(vec3, array[i]);
                                vec4 = Vec3.Vec3Max(vec4, array[i]);
                            }
                        }
                    }
                    Vec3 v = (vec3 + vec4) * 0.5f;
                    Vec3 v2 = gameEntity4.GetGlobalFrame().TransformToLocal(v);
                    MatrixFrame globalFrame2 = gameEntity4.GetGlobalFrame();
                    globalFrame2.origin -= v2;
                    itemFrame = globalFrame2;
                    MatrixFrame frame = camera.Frame;
                    float f = (vec4 - vec3).Length * 6f;
                    if (item.Type == ItemObject.ItemTypeEnum.Animal)
                    {
                        f = (vec4 - vec3).Length * 3f;
                    }
                    frame.origin += frame.rotation.u * f;
                    camera.Frame = frame;
                }
            }
            if (camera == null)
            {
                camera = Camera.CreateCamera();
                camera.SetViewVolume(false, -1f, 1f, -0.5f, 0.5f, 0.01f, 100f);
                MatrixFrame identity2 = MatrixFrame.Identity;
                identity2.origin -= identity2.rotation.u * 7f;
                identity2.rotation.u = identity2.rotation.u * -1f;
                camera.Frame = identity2;
            }
            if (item.Type == ItemObject.ItemTypeEnum.Shield)
            {
                GameEntity gameEntity5 = scene.FindEntityWithTag(tag);
                MatrixFrame holsterFrameByIndex = MBItem.GetHolsterFrameByIndex(MBItem.GetItemHolsterIndex(item.ItemHolsters[0]));
                itemFrame.rotation = holsterFrameByIndex.rotation;
                MatrixFrame frame2 = itemFrame.TransformToParent(gameEntity5.GetFrame());
                camera.Frame = frame2;
            }
        }

        private readonly ActionIndexCache act_tableau_hand_armor_pose = ActionIndexCache.Create("act_tableau_hand_armor_pose");
        
        // Token: 0x060001BD RID: 445 RVA: 0x0000E03C File Offset: 0x0000C23C
        private GameEntity AddItem(Scene scene, ItemObject item, MatrixFrame itemFrame, MatrixFrame itemFrame1, MatrixFrame itemFrame2)
        {
            ItemRosterElement rosterElement = new ItemRosterElement(item, 0, null);
            MetaMesh itemMeshForInventory = rosterElement.GetItemMeshForInventory(false);
            if (item.IsCraftedWeapon)
            {
                MatrixFrame frame = itemMeshForInventory.Frame;
                frame.Elevate(-item.WeaponDesign.CraftedWeaponLength / 2f);
                itemMeshForInventory.Frame = frame;
            }
            GameEntity gameEntity = null;
            if (itemMeshForInventory != null && rosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.HandArmor)
            {
                gameEntity = GameEntity.CreateEmpty(scene, true);
                AnimationSystemData animationSystemData = Game.Current.HumanMonster.FillAnimationSystemData(MBGlobals.PlayerMaleActionSet, 1f, false);
                AgentVisualsNativeData agentVisualsNativeData = Game.Current.HumanMonster.FillAgentVisualsNativeData();
                gameEntity.CreateSkeletonWithActionSet(ref agentVisualsNativeData, ref animationSystemData);
                gameEntity.SetFrame(ref itemFrame);
                gameEntity.Skeleton.SetAgentActionChannel(0, this.act_tableau_hand_armor_pose, 0f, -0.2f);
                gameEntity.AddMultiMeshToSkeleton(itemMeshForInventory);
                gameEntity.Skeleton.TickAnimationsAndForceUpdate(0.01f, itemFrame, true);
            }
            else if (itemMeshForInventory != null)
            {
                if (item.WeaponComponent != null)
                {
                    WeaponClass weaponClass = item.WeaponComponent.PrimaryWeapon.WeaponClass;
                    if (weaponClass == WeaponClass.ThrowingAxe || weaponClass == WeaponClass.ThrowingKnife || weaponClass == WeaponClass.Javelin || weaponClass == WeaponClass.Bolt)
                    {
                        gameEntity = GameEntity.CreateEmpty(scene, true);
                        MetaMesh metaMesh = itemMeshForInventory.CreateCopy();
                        metaMesh.Frame = itemFrame;
                        gameEntity.AddMultiMesh(metaMesh, true);
                        MetaMesh metaMesh2 = itemMeshForInventory.CreateCopy();
                        metaMesh2.Frame = itemFrame1;
                        gameEntity.AddMultiMesh(metaMesh2, true);
                        MetaMesh metaMesh3 = itemMeshForInventory.CreateCopy();
                        metaMesh3.Frame = itemFrame2;
                        gameEntity.AddMultiMesh(metaMesh3, true);
                    }
                    else
                    {
                        gameEntity = scene.AddItemEntity(ref itemFrame, itemMeshForInventory);
                    }
                }
                else
                {
                    gameEntity = scene.AddItemEntity(ref itemFrame, itemMeshForInventory);
                    if (item.Type == ItemObject.ItemTypeEnum.HorseHarness && item.ArmorComponent != null)
                    {
                        MetaMesh copy = MetaMesh.GetCopy(item.ArmorComponent.ReinsMesh, true, true);
                        if (copy != null)
                        {
                            gameEntity.AddMultiMesh(copy, true);
                        }
                    }
                }
            }
            else
            {
                MBDebug.ShowWarning("[DEBUG]Item with " + rosterElement.EquipmentElement.Item.StringId + "[DEBUG] string id cannot be found");
            }
            gameEntity.SetVisibilityExcludeParents(false);
            return gameEntity;
        }
		
        private static void GetItemPoseAndCameraForCraftedItem(ItemObject item, Scene scene, ref Camera camera, ref MatrixFrame itemFrame, ref MatrixFrame itemFrame1, ref MatrixFrame itemFrame2)
        {
            if (camera == null)
            {
                camera = Camera.CreateCamera();
            }
            itemFrame = MatrixFrame.Identity;
            WeaponClass weaponClass = item.WeaponDesign.Template.WeaponDescriptions.First().WeaponClass;
            Vec3 u = itemFrame.rotation.u;
            Vec3 v = itemFrame.origin - u * (item.WeaponDesign.CraftedWeaponLength * 0.5f);
            Vec3 v2 = v + u * item.WeaponDesign.CraftedWeaponLength;
            Vec3 v3 = v - u * item.WeaponDesign.BottomPivotOffset;
            int num = 0;
            Vec3 v4 = default(Vec3);
            foreach (float num2 in item.WeaponDesign.TopPivotOffsets)
            {
                if (num2 > Math.Abs(1E-05f))
                {
                    Vec3 vec = v + u * num2;
                    if (num == 1)
                    {
                        v4 = vec;
                    }
                    num++;
                }
            }
            if (weaponClass == WeaponClass.OneHandedSword || weaponClass == WeaponClass.TwoHandedSword)
            {
                GameEntity gameEntity = scene.FindEntityWithTag("sword_camera");
                Vec3 vec2 = default(Vec3);
                gameEntity.GetCameraParamsFromCameraScript(camera, ref vec2);
                gameEntity.SetVisibilityExcludeParents(false);
                Vec3 v5 = itemFrame.TransformToLocal(v3);
                MatrixFrame identity = MatrixFrame.Identity;
                identity.origin = -v5;
                GameEntity gameEntity2 = scene.FindEntityWithTag("sword");
                gameEntity2.SetVisibilityExcludeParents(false);
                itemFrame = gameEntity2.GetGlobalFrame();
                itemFrame = itemFrame.TransformToParent(identity);
            }
            if (weaponClass == WeaponClass.OneHandedAxe || weaponClass == WeaponClass.TwoHandedAxe)
            {
                GameEntity gameEntity3 = scene.FindEntityWithTag("axe_camera");
                Vec3 vec3 = default(Vec3);
                gameEntity3.GetCameraParamsFromCameraScript(camera, ref vec3);
                gameEntity3.SetVisibilityExcludeParents(false);
                Vec3 v6 = itemFrame.TransformToLocal(v4);
                MatrixFrame identity2 = MatrixFrame.Identity;
                identity2.origin = -v6;
                GameEntity gameEntity4 = scene.FindEntityWithTag("axe");
                gameEntity4.SetVisibilityExcludeParents(false);
                itemFrame = gameEntity4.GetGlobalFrame();
                itemFrame = itemFrame.TransformToParent(identity2);
            }
            if (weaponClass == WeaponClass.Dagger)
            {
                GameEntity gameEntity5 = scene.FindEntityWithTag("sword_camera");
                Vec3 vec4 = default(Vec3);
                gameEntity5.GetCameraParamsFromCameraScript(camera, ref vec4);
                gameEntity5.SetVisibilityExcludeParents(false);
                Vec3 v7 = itemFrame.TransformToLocal(v3);
                MatrixFrame identity3 = MatrixFrame.Identity;
                identity3.origin = -v7;
                GameEntity gameEntity6 = scene.FindEntityWithTag("sword");
                gameEntity6.SetVisibilityExcludeParents(false);
                itemFrame = gameEntity6.GetGlobalFrame();
                itemFrame = itemFrame.TransformToParent(identity3);
            }
            if (weaponClass == WeaponClass.ThrowingAxe)
            {
                GameEntity gameEntity7 = scene.FindEntityWithTag("throwing_axe_camera");
                Vec3 vec5 = default(Vec3);
                gameEntity7.GetCameraParamsFromCameraScript(camera, ref vec5);
                gameEntity7.SetVisibilityExcludeParents(false);
                Vec3 v8 = v + u * item.PrimaryWeapon.CenterOfMass;
                Vec3 v9 = itemFrame.TransformToLocal(v8);
                MatrixFrame identity4 = MatrixFrame.Identity;
                identity4.origin = -v9 * 2.5f;
                GameEntity gameEntity8 = scene.FindEntityWithTag("throwing_axe");
                gameEntity8.SetVisibilityExcludeParents(false);
                itemFrame = gameEntity8.GetGlobalFrame();
                itemFrame = itemFrame.TransformToParent(identity4);
                gameEntity8 = scene.FindEntityWithTag("throwing_axe_1");
                gameEntity8.SetVisibilityExcludeParents(false);
                itemFrame1 = gameEntity8.GetGlobalFrame();
                itemFrame1 = itemFrame1.TransformToParent(identity4);
                gameEntity8 = scene.FindEntityWithTag("throwing_axe_2");
                gameEntity8.SetVisibilityExcludeParents(false);
                itemFrame2 = gameEntity8.GetGlobalFrame();
                itemFrame2 = itemFrame2.TransformToParent(identity4);
            }
            if (weaponClass == WeaponClass.Javelin)
            {
                GameEntity gameEntity9 = scene.FindEntityWithTag("javelin_camera");
                Vec3 vec6 = default(Vec3);
                gameEntity9.GetCameraParamsFromCameraScript(camera, ref vec6);
                gameEntity9.SetVisibilityExcludeParents(false);
                Vec3 v10 = itemFrame.TransformToLocal(v4);
                MatrixFrame identity5 = MatrixFrame.Identity;
                identity5.origin = -v10 * 2.2f;
                GameEntity gameEntity10 = scene.FindEntityWithTag("javelin");
                gameEntity10.SetVisibilityExcludeParents(false);
                itemFrame = gameEntity10.GetGlobalFrame();
                itemFrame = itemFrame.TransformToParent(identity5);
                gameEntity10 = scene.FindEntityWithTag("javelin_1");
                gameEntity10.SetVisibilityExcludeParents(false);
                itemFrame1 = gameEntity10.GetGlobalFrame();
                itemFrame1 = itemFrame1.TransformToParent(identity5);
                gameEntity10 = scene.FindEntityWithTag("javelin_2");
                gameEntity10.SetVisibilityExcludeParents(false);
                itemFrame2 = gameEntity10.GetGlobalFrame();
                itemFrame2 = itemFrame2.TransformToParent(identity5);
            }
            if (weaponClass == WeaponClass.ThrowingKnife)
            {
                GameEntity gameEntity11 = scene.FindEntityWithTag("javelin_camera");
                Vec3 vec7 = default(Vec3);
                gameEntity11.GetCameraParamsFromCameraScript(camera, ref vec7);
                gameEntity11.SetVisibilityExcludeParents(false);
                Vec3 v11 = itemFrame.TransformToLocal(v2);
                MatrixFrame identity6 = MatrixFrame.Identity;
                identity6.origin = -v11 * 1.4f;
                GameEntity gameEntity12 = scene.FindEntityWithTag("javelin");
                gameEntity12.SetVisibilityExcludeParents(false);
                itemFrame = gameEntity12.GetGlobalFrame();
                itemFrame = itemFrame.TransformToParent(identity6);
                gameEntity12 = scene.FindEntityWithTag("javelin_1");
                gameEntity12.SetVisibilityExcludeParents(false);
                itemFrame1 = gameEntity12.GetGlobalFrame();
                itemFrame1 = itemFrame1.TransformToParent(identity6);
                gameEntity12 = scene.FindEntityWithTag("javelin_2");
                gameEntity12.SetVisibilityExcludeParents(false);
                itemFrame2 = gameEntity12.GetGlobalFrame();
                itemFrame2 = itemFrame2.TransformToParent(identity6);
            }
            if (weaponClass == WeaponClass.TwoHandedPolearm || weaponClass == WeaponClass.OneHandedPolearm || weaponClass == WeaponClass.LowGripPolearm || weaponClass == WeaponClass.Mace || weaponClass == WeaponClass.TwoHandedMace)
            {
                GameEntity gameEntity13 = scene.FindEntityWithTag("spear_camera");
                Vec3 vec8 = default(Vec3);
                gameEntity13.GetCameraParamsFromCameraScript(camera, ref vec8);
                gameEntity13.SetVisibilityExcludeParents(false);
                Vec3 v12 = itemFrame.TransformToLocal(v4);
                MatrixFrame identity7 = MatrixFrame.Identity;
                identity7.origin = -v12;
                GameEntity gameEntity14 = scene.FindEntityWithTag("spear");
                gameEntity14.SetVisibilityExcludeParents(false);
                itemFrame = gameEntity14.GetGlobalFrame();
                itemFrame = itemFrame.TransformToParent(identity7);
            }
        }
    }
}
#endif
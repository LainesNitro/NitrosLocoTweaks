using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using CarChanger.Game;
using CarChanger.Game.Components;
using UnityEngine;

namespace S282ModelTweaks
{
    public class DrivetrainChanger
    {
        private MaterialHolder _matHolder;
        private Transform _drivetrainL;
        private Transform _drivetrainR;
        private List<GameObject> _origGOs;
        private List<GameObject> _newGOs;
        private List<ExplosionModelHandler?> _explosionModelHandlers;

        private TrainCar Car => _matHolder.Car;


        public DrivetrainChanger(MaterialHolder holder, GameObject? drivetrainPrefab)
        {
            _matHolder = holder;
            _drivetrainL = Car.transform.Find("LocoS282A_Body/MovingParts_LOD0/LocoS282A_Drivetrain L");
            _drivetrainR = Car.transform.Find("LocoS282A_Body/MovingParts_LOD0/LocoS282A_Drivetrain R");
            _origGOs = [];
            _newGOs = [];
            _explosionModelHandlers = [];

            if (drivetrainPrefab == null)
            {
                Main.Instance.Logger.Error("Body prefab is null!");
                return;
            }

            SetDrivetrain(_drivetrainL, drivetrainPrefab);
            SetDrivetrain(_drivetrainR, drivetrainPrefab);
        }
        
        private void SetDrivetrain(Transform drivetrain, GameObject gearPrefab)
        {
            var drivetrainTransforms = drivetrain.GetComponentsInChildren<Transform>();
            
            foreach (GameObject modObj in gearPrefab.transform.AllChildGOs())
            {
                var origObj = drivetrainTransforms.Where(c => c.name == modObj.name).FirstOrDefault().gameObject;

                if (origObj == null)
                {
                    Main.Instance.Logger.Warning("Cannot find " + modObj.name + "!");
                    continue;
                }

                if (!origObj.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    Main.Instance.Logger.Warning("Cannot find MeshRenderer in " + origObj.name + "!");
                    continue;
                }

                meshRenderer.enabled = false;
                SetActiveLOD1(origObj, false);

                _origGOs.Add(origObj);


                var newObj = UnityEngine.Object.Instantiate(modObj, origObj.transform);

                ComponentProcessor.ProcessComponentsMinimal(newObj, _matHolder);

                _explosionModelHandlers.Add(CarChangerExplosionManager.PrepareExplosionHandler(newObj, _matHolder));

                _newGOs.Add(newObj);
            }
        }

        private void SetActiveLOD1(GameObject go, bool active)
        {
            var transforms = go.GetComponentsInChildren<Transform>(true).Where(t => t.name == (go.name + "_LOD1"));
            
            if (transforms == null || !transforms.Any())
            {
                Main.Instance.Logger.Warning("Cannot find LOD of " + go.name + "!");
                return;
            }

            transforms.FirstOrDefault().gameObject.SetActive(active);
        }

        public void Reset()
        {
            foreach (var origGO in _origGOs)
            {
                if (!origGO.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    Main.Instance.Logger.Warning("Cannot find MeshRenderer in " + origGO.name + "!");
                    continue;
                }

                meshRenderer.enabled = true;
                SetActiveLOD1(origGO, true);
            }

            foreach (var newGO in _newGOs)
            {
                UnityEngine.Object.Destroy(newGO);
            }

            foreach (var handler in _explosionModelHandlers)
            {
                Helpers.DestroyGameObjectIfNotNull(handler);
            }
        }

    }
}

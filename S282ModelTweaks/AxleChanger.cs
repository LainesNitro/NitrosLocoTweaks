using System;
using System.Collections.Generic;
using System.Linq;
using CarChanger.Common.Components;
using CarChanger.Game;
using CarChanger.Game.Components;
using DV.Wheels;
using UnityEngine;

namespace S282ModelTweaks
{
    internal class AxleChanger
    {
        private MaterialHolder _matHolder;
        private Transform _originalF;
        private Transform _originalR;
        private Transform _originalFLOD;
        private Transform _originalRLOD;
        private Transform? _newF;
        private Transform? _newR;
        private float _originalRadiusF;
        private float _originalRadiusR;
        private Vector3 _originalAxlePosF;
        private Vector3 _originalAxlePosR;
        private ExplosionModelHandler? _explosionManagerF;
        private ExplosionModelHandler? _explosionManagerR;

        private TrainCar Car => _matHolder.Car;

        public AxleChanger(MaterialHolder holder, GameObject? axlesPrefab)
        {
            _matHolder = holder;

            var bogieF = Car.Bogies[1];
            var bogieR = Car.Bogies[0];

            // Invalidate cached axles.
            Helpers.InvalidateBogieCache(bogieF);
            Helpers.InvalidateBogieCache(bogieR);

            _originalF = bogieF.transform.Find("bogie_car/[axle]/axleF_model");
            _originalR = bogieR.transform.Find("bogie_car/[axle]/axleR_model");
            _originalFLOD = bogieF.transform.Find("bogie_car/[axle]/axleF_modelLOD1");
            _originalRLOD = bogieR.transform.Find("bogie_car/[axle]/axleR_modelLOD1");

            _originalRadiusF = GetWheelRadius(_originalF.parent);
            _originalRadiusR = GetWheelRadius(_originalR.parent);

            _originalAxlePosF = _originalF.parent.localPosition;
            _originalAxlePosR = _originalR.parent.localPosition;

            if (axlesPrefab == null)
            {
                Main.Instance.Logger.Error("Unable to find Axles prefab!");
                return;
            }

            var axleF = axlesPrefab.transform.Find("axleF_model");
            var axleR = axlesPrefab.transform.Find("axleR_model");

            if (axleF == null)
            {
                Main.Instance.Logger.Warning("Unable to find axle!");
            }
            else
            {

                if (!_originalF.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    Main.Instance.Logger.Warning("Cannot find MeshRenderer in " + _originalF.name + "!");
                }
                else
                {
                    meshRenderer.enabled = false;
                }

                _newF = UnityEngine.Object.Instantiate(axleF.gameObject, _originalF).transform;

                _originalF.parent.localPosition = _newF.localPosition;

                SetWheelRadius(_originalF.parent, _newF.localPosition.y);

                _newF.localPosition = Vector3.zero;

                _originalFLOD.gameObject.SetActive(false);

                ComponentProcessor.ProcessComponentsMinimal(_newF.gameObject, _matHolder);

                _explosionManagerF = CarChangerExplosionManager.PrepareExplosionHandler(_newF.gameObject, _matHolder);
            }

            if (axleR == null)
            {
                Main.Instance.Logger.Warning("Unable to find axleR_model!");
            }
            else
            {
                if (!_originalR.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    Main.Instance.Logger.Warning("Cannot find MeshRenderer in " + _originalR.name + "!");
                }
                else
                {
                    meshRenderer.enabled = false;
                }

                _newR = UnityEngine.Object.Instantiate(axleR.gameObject, _originalR).transform;

                _originalR.parent.localPosition = _newR.localPosition;

                SetWheelRadius(_originalR.parent, _newR.localPosition.y);

                _newR.localPosition = Vector3.zero;

                _originalRLOD.gameObject.SetActive(false);

                ComponentProcessor.ProcessComponentsMinimal(_newR.gameObject, _matHolder);

                _explosionManagerR = CarChangerExplosionManager.PrepareExplosionHandler(_newR.gameObject, _matHolder);
            }
        }

        public void Reset()
        {
            var bogieF = Car.Bogies[1];
            var bogieR = Car.Bogies[0];

            if (_newF != null)
            {
                _newF.name = "[destroyed]";
                UnityEngine.Object.Destroy(_newF.gameObject);
                _newF = null;
                
                if (_originalF.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    meshRenderer.enabled = true;
                }

                SetWheelRadius(_originalF.parent, _originalRadiusF);

                _originalF.parent.localPosition = _originalAxlePosF;

                _originalFLOD.gameObject.SetActive(true);
            }

            if (_newR != null)
            {
                _newR.name = "[destroyed]";
                UnityEngine.Object.Destroy(_newR.gameObject);
                _newR = null;

                if (_originalR.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    meshRenderer.enabled = true;
                }

                SetWheelRadius(_originalR.parent, _originalRadiusR);

                _originalR.parent.localPosition = _originalAxlePosR;

                _originalRLOD.gameObject.SetActive(true);
            }

            Helpers.InvalidateBogieCache(bogieF);
            Helpers.InvalidateBogieCache(bogieR);

            Helpers.DestroyGameObjectIfNotNull(_explosionManagerF);
            Helpers.DestroyGameObjectIfNotNull(_explosionManagerR);
        }

        private float GetWheelRadius(Transform transformToRotate)
        {
            WheelRotationViaCode[] wheelComps = Car.GetComponentsInChildren<WheelRotationViaCode>();
            var wheelRotationViaCode = wheelComps.Where(w => w.transformsToRotate[0] == transformToRotate).FirstOrDefault();
            if (wheelRotationViaCode == null)
            {
                Main.Instance.Logger.Warning("Cannot find WheelRotationViaCode component!");
                return 0;
            }
            return wheelRotationViaCode.wheelRadius;
        }

        private void SetWheelRadius(Transform transformToRotate, float radius)
        {
            WheelRotationViaCode[] wheelComps = Car.GetComponentsInChildren<WheelRotationViaCode>();
            var wheelRotationViaCode = wheelComps.Where(w => w.transformsToRotate[0] == transformToRotate).FirstOrDefault();
            if (wheelRotationViaCode == null)
            {
                Main.Instance.Logger.Warning("Cannot find WheelRotationViaCode component!");
                return;
            }
            wheelRotationViaCode.wheelRadius = radius;
            wheelRotationViaCode.wheelCircumference = radius * 2 * Mathf.PI;
        }
    }
}

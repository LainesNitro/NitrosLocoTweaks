using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CarChanger.Common.Components;
using CarChanger.Game;
using CarChanger.Game.Components;
using DV.Wheels;
using LocoSim.Implementations.Wheels;
using UnityEngine;

namespace S282ModelTweaks
{
    public class S282ABogieChanger
    {
        private MaterialHolder _matHolder;
        private Transform _originalF;
        private Transform _originalR;
        private GameObject? _newF;
        private GameObject? _newR;
        private float _radiusF;
        private float _radiusR;
        private ExplosionModelHandler? _explosionManagerF;
        private ExplosionModelHandler? _explosionManagerR;

        private TrainCar Car => _matHolder.Car;

        public S282ABogieChanger(MaterialHolder holder, GameObject? frontBogie, GameObject? rearBogie)
        {
            _matHolder = holder;
            _radiusF = 0.355f;
            _radiusR = 0.575f;

            // Store them for quick access.
            var bogies = Car.Bogies;
            var bogieF = bogies[1];
            var bogieR = bogies[0];

            // Invalidate cached axles.
            Helpers.InvalidateBogieCache(bogieF);
            Helpers.InvalidateBogieCache(bogieR);

            _originalF = bogieF.transform.GetChild(0);
            _originalR = bogieR.transform.GetChild(0);

            if (frontBogie != null)
            {
                _radiusF = frontBogie.transform.Find("[axle]/axleF_model").parent.localPosition.y;

                _newF = UnityEngine.Object.Instantiate(frontBogie, bogieF.transform);
                _newF.name = "bogie_car";

                _originalF.name = "[replaced]";
                _originalF.gameObject.SetActive(false);

                ComponentProcessor.ProcessComponentsMinimal(_newF, _matHolder);
                _explosionManagerF = CarChangerExplosionManager.PrepareExplosionHandler(_newF, _matHolder);
            }

            if (rearBogie != null)
            {
                _radiusR = rearBogie.transform.Find("[axle]/axleR_model").parent.localPosition.y;

                _newR = UnityEngine.Object.Instantiate(rearBogie, bogieR.transform);
                _newR.name = "bogie_car";

                _originalR.name = "[replaced]";
                _originalR.gameObject.SetActive(false);

                ComponentProcessor.ProcessComponentsMinimal(_newR, _matHolder);
                _explosionManagerR = CarChangerExplosionManager.PrepareExplosionHandler(_newR, _matHolder);
            }

            CommonPoweredProcedure(_radiusF, _radiusR);
        }

        public void Reset()
        {
            var bogies = Car.Bogies;
            var bogieF = bogies[1];
            var bogieR = bogies[0];

            _originalF.name = "bogie_car";
            _originalR.name = "bogie_car";
            _originalF.gameObject.SetActive(true);
            _originalR.gameObject.SetActive(true);

            if (_newF != null)
            {
                _newF.name = "[destroyed]";
                UnityEngine.Object.Destroy(_newF);
            }
            if (_newR != null)
            {
                _newR.name = "[destroyed]";
                UnityEngine.Object.Destroy(_newR);
            }

            Helpers.InvalidateBogieCache(bogieF);
            Helpers.InvalidateBogieCache(bogieR);

            Helpers.DestroyGameObjectIfNotNull(_explosionManagerF);
            Helpers.DestroyGameObjectIfNotNull(_explosionManagerR);

            CommonPoweredProcedure(0.355f, 0.575f);
        }

        private void CommonPoweredProcedure(float radiusF, float radiusR)
        {
            var wheelRotations = Car.gameObject.GetComponentsInChildren<WheelRotationViaCode>();

            if (wheelRotations == null || !wheelRotations.Any())
                return;

            var manager = Car.GetComponentInChildren<PoweredWheelsManager>();

            // Can't make them powered in this case.
            if (!manager) return;

            var wheelStates = manager.poweredWheels
                .OrderBy(x => -Car.transform.InverseTransformPoint(x.transform.position).z)
                .Select(x => x.GetComponent<PoweredWheel>().state).ToArray();

            
            // Get all axles, ordered by position in relation to the car for consistency.
            var axles = Car.Bogies.SelectMany(x => x.Axles).Select(x => x.transform).OrderBy(x => -Car.transform.InverseTransformPoint(x.position).z);
            
            wheelRotations[0].transformsToRotate = [axles.ElementAt(0)];
            wheelRotations[0].wheelRadius = radiusF;
            wheelRotations[0].wheelCircumference = radiusF * 2 * Mathf.PI;

            wheelRotations[1].transformsToRotate = [axles.ElementAt(5)];
            wheelRotations[1].wheelRadius = radiusR;
            wheelRotations[1].wheelCircumference = radiusR * 2 * Mathf.PI;

            int i = 0;
            List<PoweredWheel> powered = [];

            foreach (var item in axles)
            {
                if (item.TryGetComponent<PoweredWheel>(out var wheel))
                {
                    wheel.state = wheelStates[i++];
                    powered.Add(wheel);
                    continue;
                }
                if (item.TryGetComponent<PoweredAxle>(out var axle))
                {
                    wheel = item.gameObject.AddComponent<PoweredWheel>();
                    wheel.wheelTransform = item;
                    wheel.localRotationAxis = axle.Axis;
                    wheel.state = wheelStates[i++];
                    powered.Add(wheel);
                    UnityEngine.Object.Destroy(axle);
                    continue;
                }
            }

            manager.poweredWheels = powered.ToArray();
        }
    }
}

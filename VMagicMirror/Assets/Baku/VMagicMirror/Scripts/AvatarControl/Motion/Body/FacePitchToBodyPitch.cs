﻿using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>顔のピッチ角を体のピッチ角にすり替えるすごいやつだよ </summary>
    public sealed class FacePitchToBodyPitch
    {
        //最終的に胴体ピッチは頭ピッチの何倍であるべきか、という値
        private const float GoalRate = -0.05f;
        //ゴールに持ってくときの速度基準にする時定数っぽいやつ
        private const float TimeFactor = 0.2f;
        //ゴール回転値に持ってくとき、スピードに掛けるダンピング項
        private const float SpeedDumpFactor = 0.95f;
        //ゴール回転値に持っていくとき、スピードをどのくらい素早く適用するか
        private const float SpeedLerpFactor = 12.0f;
        
        private float _speedDegreePerSec = 0;
        public float PitchAngleDegree { get; private set; }

        //NOTE: この値はフィルタされてない生のやつ
        private float _targetAngleDegree = 0;

        public void UpdateSuggestAngle()
        {
            //やること: headRollをbodyRollに変換し、それをQuaternionとして人に見せられる形にする
            float idealSpeed = (_targetAngleDegree * GoalRate - PitchAngleDegree) / TimeFactor;
            _speedDegreePerSec = Mathf.Lerp(
                _speedDegreePerSec,
                idealSpeed,
                SpeedLerpFactor * Time.deltaTime
            );

            _speedDegreePerSec *= SpeedDumpFactor;
            PitchAngleDegree += _speedDegreePerSec * Time.deltaTime;
        }

        public void SetZeroTarget()
        {
            _targetAngleDegree = 0;
        }

        public void CheckAngle(Quaternion headRotation)
        {
            //ピッチはforwardが上がった/下がったの話に帰着すればOK。下向きが正なことに注意
            var rotatedForward = headRotation * Vector3.forward;
            _targetAngleDegree = Mathf.Asin(rotatedForward.y) * Mathf.Rad2Deg;            
        }
    }
}

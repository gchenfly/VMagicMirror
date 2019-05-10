﻿using System;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class SettingAutoAdjuster : MonoBehaviour
    {
        //基準長はMegumi Baxterさんの体型。(https://hub.vroid.com/characters/9003440353945198963/models/7418874241157618732)
        //UpperArm to Hand
        private const float ReferenceArmLength = 0.378f;
        //Hand (Wrist) to Middle Distal
        private const float ReferenceHandLength = 0.114f;

        [SerializeField]
        private ReceivedMessageHandler handler;

        [SerializeField]
        private GrpcSender sender;

        [SerializeField]
        private BlendShapeAssignController blendShapeAssignController;

        [SerializeField]
        private Transform cam;

        private Transform _vrmRoot = null;

        public void AssignModelRoot(Transform vrmRoot)
        {
            _vrmRoot = vrmRoot;
        }

        public void DisposeModelRoot()
        {
            _vrmRoot = null;
        }

        // Start is called before the first frame update
        private void Start()
        {
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.RequestAutoAdjust:
                        AutoAdjust();
                        break;
                    default:
                        break;
                }
            });
        }

        private void AutoAdjust()
        {
            if (_vrmRoot == null) { return; }

            var parameters = new AutoAdjustParameters();
            //やること: 
            //1. いま読まれてるモデルの体型からいろんなパラメータを決めてparametersに入れていく
            //2. 決定したパラメータが疑似的にメッセージハンドラから飛んできたことにして適用
            //3. 決定したパラメータをコンフィグ側に送る

            try
            {
                var animator = _vrmRoot.GetComponent<Animator>();

                //3つのサブルーチンではanimatorのHumanoidBoneを使うが、部位である程度分けられるので分けておく
                SetHandSizeRelatedParameters(animator, parameters);
                SetArmLengthRelatedParameters(animator, parameters);
                SetBodyHeightRelatedParameters(animator, parameters);
                //眉毛はブレンドシェイプ
                SetEyebrowParameters(parameters);
                AdjustCameraPosition(animator);

                SendParameterRelatedCommands(parameters);

                //3. 決定したパラメータをコンフィグ側に送る
                sender.SendCommand(MessageFactory.Instance.AutoAdjustResults(parameters));
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void SendParameterRelatedCommands(AutoAdjustParameters parameters)
        {
            var commands = new ReceivedCommand[]
            {
                #region Motion
                new ReceivedCommand(
                    MessageCommandNames.EyebrowLeftUpKey,
                    parameters.EyebrowLeftUpKey
                    ),
                new ReceivedCommand(
                    MessageCommandNames.EyebrowLeftDownKey,
                    parameters.EyebrowLeftDownKey
                    ),
                new ReceivedCommand(
                    MessageCommandNames.UseSeparatedKeyForEyebrow,
                    $"{parameters.UseSeparatedKeyForEyebrow}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.EyebrowRightUpKey,
                    parameters.EyebrowRightUpKey
                    ),
                new ReceivedCommand(
                    MessageCommandNames.EyebrowRightDownKey,
                    parameters.EyebrowRightDownKey
                    ),
                new ReceivedCommand(
                    MessageCommandNames.EyebrowUpScale,
                    $"{parameters.EyebrowUpScale}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.EyebrowDownScale,
                    $"{parameters.EyebrowDownScale}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.LengthFromWristToPalm,
                    $"{parameters.LengthFromWristToPalm}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.LengthFromWristToTip,
                    $"{parameters.LengthFromWristToTip}"
                    ),
                #endregion
                #region Layout
                new ReceivedCommand(
                    MessageCommandNames.HidHeight,
                    $"{parameters.HidHeight}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.HidHorizontalScale,
                    $"{parameters.HidHorizontalScale}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.GamepadHeight,
                    $"{parameters.GamepadHeight}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.GamepadHorizontalScale,
                    $"{parameters.GamepadHorizontalScale}"
                    ),
                #endregion
            };

            //いまのとこ全てが単なるValue Setterなので即時で処理させて大丈夫
            foreach(var cmd in commands)
            {
                handler.ReceiveCommand(cmd);
            }
        }

        private void AdjustCameraPosition(Animator animator)
        {
            var head = animator.GetBoneTransform(HumanBodyBones.Neck);
            cam.position = new Vector3(0, head.position.y, 1);
            cam.rotation = Quaternion.Euler(0, 180, 0);
        }

        private void SetEyebrowParameters(AutoAdjustParameters parameters)
        {
            var blendShapeNames = blendShapeAssignController.TryGetBlendShapeNames();
            var adjuster = new EyebrowBlendShapeAdjuster(blendShapeNames);
            var settings = adjuster.CreatePreferredSettings();
            parameters.EyebrowLeftUpKey = settings.EyebrowLeftUpKey;
            parameters.EyebrowLeftDownKey = settings.EyebrowLeftDownKey;
            parameters.UseSeparatedKeyForEyebrow = settings.UseSeparatedKeyForEyebrow;
            parameters.EyebrowRightUpKey = settings.EyebrowRightUpKey;
            parameters.EyebrowRightDownKey = settings.EyebrowRightDownKey;
            parameters.EyebrowUpScale = settings.EyebrowUpScale;
            parameters.EyebrowDownScale = settings.EyebrowDownScale;
        }

        private void SetBodyHeightRelatedParameters(Animator animator, AutoAdjustParameters parameters)
        {
            var chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
            parameters.HidHeight = Mathf.RoundToInt(chestBone.position.y * 100);
            parameters.GamepadHeight = parameters.HidHeight + 5;
        }

        private void SetArmLengthRelatedParameters(Animator animator, AutoAdjustParameters parameters)
        {
            var upperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var lowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var wrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
            float armLength =
                Vector3.Distance(upperArm.position, lowerArm.position) +
                Vector3.Distance(lowerArm.position, wrist.position);

            float factor = armLength / ReferenceArmLength;
            parameters.HidHorizontalScale = (int)(parameters.HidHorizontalScale * factor);
            parameters.GamepadHorizontalScale = (int)(parameters.GamepadHorizontalScale * factor);
        }

        private void SetHandSizeRelatedParameters(Animator animator, AutoAdjustParameters parameters)
        {
            var tip = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
            if (tip == null) { return; }

            var wrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
            float distance = Vector3.Distance(tip.position, wrist.position);

            float factor = distance / ReferenceHandLength;

            parameters.LengthFromWristToPalm = (int)(parameters.LengthFromWristToPalm * factor);
            parameters.LengthFromWristToTip = (int)(parameters.LengthFromWristToTip * factor);

        }



    }
}


﻿using Deform;
using DG.Tweening;
using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(MagnetDeformer))]
    public class GamepadVisibilityReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler _handler;

        private MagnetDeformer _deformer = null;
        private Renderer[] _renderers = new Renderer[0];
        
        private void Start()
        {
            _handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.GamepadVisibility:
                        SetGamepadVisibility(message.ToBoolean());
                        break;
                }
            });

            _deformer = GetComponent<MagnetDeformer>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        private void SetGamepadVisibility(bool visible)
        {
            DOTween
                .To(
                    () => _deformer.Factor, 
                    v => _deformer.Factor = v, 
                    visible ? 0.0f : 0.5f, 
                    0.5f)
                .SetEase(Ease.OutCubic)
                .OnStart(() =>
                {
                    if (visible)
                    {
                        foreach (var r in _renderers)
                        {
                            r.enabled = true;
                        }
                    }
                })
                .OnComplete(() =>
                {
                    foreach (var r in _renderers)
                    {
                        r.enabled = visible;
                    }
                });
        }
    }
}

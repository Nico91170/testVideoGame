using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Rive404.Player
{
    /// <summary>Uses the original FBX idle/walk clips without requiring an editor-created controller.</summary>
    public sealed class LocomotionAnimation : MonoBehaviour
    {
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _idle;
        private AnimationClipPlayable _walk;
        private float _idleLength;
        private float _walkLength;
        private float _idleTime;
        private float _walkTime;
        private float _targetSpeed;
        private float _weight;

        public static LocomotionAnimation Attach(Transform model, string resourceName)
        {
            if (model == null) return null;
            AnimationClip idle = null;
            AnimationClip walk = null;
            foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>("Models/" + resourceName))
            {
                if (clip.name.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0) idle = clip;
                if (clip.name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0) walk = clip;
            }
            if (idle == null || walk == null || idle.legacy || walk.legacy) return null;
            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            LocomotionAnimation animation = model.gameObject.AddComponent<LocomotionAnimation>();
            animation.Create(animator, idle, walk);
            return animation;
        }

        private void Create(Animator animator, AnimationClip idleClip, AnimationClip walkClip)
        {
            _idleLength = Mathf.Max(0.01f, idleClip.length);
            _walkLength = Mathf.Max(0.01f, walkClip.length);
            _graph = PlayableGraph.Create("RIVE404 " + name + " locomotion");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            _idle = AnimationClipPlayable.Create(_graph, idleClip);
            _walk = AnimationClipPlayable.Create(_graph, walkClip);
            // Loop manually, including imported clips that have no loopTime setting.
            _idle.SetSpeed(0);
            _walk.SetSpeed(0);
            _graph.Connect(_idle, 0, _mixer, 0);
            _graph.Connect(_walk, 0, _mixer, 1);
            _mixer.SetInputWeight(0, 1f);
            _mixer.SetInputWeight(1, 0f);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Body", animator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
        }

        public void SetSpeed(float metresPerSecond)
        {
            _targetSpeed = Mathf.Max(0f, metresPerSecond);
        }

        private void Update()
        {
            if (!_graph.IsValid()) return;
            _weight = Mathf.MoveTowards(_weight, Mathf.Clamp01(_targetSpeed / 1.5f), Time.deltaTime * 6f);
            _idleTime = Mathf.Repeat(_idleTime + Time.deltaTime, _idleLength);
            _walkTime = Mathf.Repeat(_walkTime + Time.deltaTime * Mathf.Clamp(_targetSpeed / 4f, 0.65f, 2f),
                _walkLength);
            _idle.SetTime(_idleTime);
            _walk.SetTime(_walkTime);
            _mixer.SetInputWeight(0, 1f - _weight);
            _mixer.SetInputWeight(1, _weight);
        }

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        private void OnDisable()
        {
            if (_graph.IsValid()) _graph.Stop();
        }

        private void OnEnable()
        {
            if (_graph.IsValid()) _graph.Play();
        }
    }
}

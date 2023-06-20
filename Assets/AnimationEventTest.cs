using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

// About this issue:
// The Animation Event will be triggered twice in certain situations.
// 
// How to reproduce:
// 1. Open the "Sample" scene.
// 2. Enter the Play mode.
// 3. In the Game view, click the "Perform" button.
// 4. In the Game view, observe the "Event Counter" text.
// Expected result: The "Event Counter" value should be "1".
// Actual result: The "Event Counter" value is "2".

public class MixerCtrlBehaviour : PlayableBehaviour
{
    public AnimationClipPlayable idleAcp;
    public AnimationClipPlayable walkAcp;
    public AnimationClipPlayable runAcp;
    public AnimationMixerPlayable mixer;
    public bool perform;

    private ulong _frame;

    public override void PrepareFrame(Playable playable, FrameData info)
    {
        base.PrepareFrame(playable, info);

        if (!perform) return;

        switch (_frame)
        {
            case 0:
                mixer.SetInputWeight(0, 0.9f);
                mixer.ConnectInput(1, walkAcp, 0, 0.1f);
                walkAcp.SetTime(0); // IMPORTANT: This is the line that causes the issue
                mixer.SetInputWeight(2, 0.0f);
                _frame++;
                break;

            case 1:
                mixer.SetInputWeight(0, 0.9f);
                mixer.SetInputWeight(1, 0.05f);
                // mixer.SetInputWeight(2, 0.05f);
                mixer.ConnectInput(2, runAcp, 0, 0.05f); // IMPORTANT: This is the line that causes the issue
                _frame++;
                break;

            default:
                _frame++;
                break;
        }
    }
}

[RequireComponent(typeof(Animator))]
public class AnimationEventTest : MonoBehaviour
{
    public Text eventCounterText;
    public AnimationClip idleClip;
    public AnimationClip walkClip;
    public AnimationClip runClip;

    private Animator _animator;
    private PlayableGraph _graph;
    private MixerCtrlBehaviour _mixerCtrl;
    private ulong _eventCounter;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _graph = PlayableGraph.Create(GetType().Name);
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var sp = ScriptPlayable<MixerCtrlBehaviour>.Create(_graph, 1);
        _mixerCtrl = sp.GetBehaviour();

        var mixer = AnimationMixerPlayable.Create(_graph, 3);
        sp.ConnectInput(0, mixer, 0, 1f);

        _mixerCtrl.mixer = mixer;
        _mixerCtrl.idleAcp = AnimationClipPlayable.Create(_graph, idleClip);
        _mixerCtrl.walkAcp = AnimationClipPlayable.Create(_graph, walkClip);
        _mixerCtrl.runAcp = AnimationClipPlayable.Create(_graph, runClip);

        mixer.ConnectInput(0, _mixerCtrl.idleAcp, 0, 1f);

        var output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
        output.SetSourcePlayable(sp);

        _graph.Play();
    }

    private void OnDestroy()
    {
        if (_graph.IsValid()) _graph.Destroy();
    }

    // Button Click
    public void Perform()
    {
        _mixerCtrl.perform = true;
    }

    // Animation Event
    private void WalkFrame0()
    {
        _eventCounter++;
        eventCounterText.text = $"Event Counter: {_eventCounter}";
    }
}
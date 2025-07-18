using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
public struct InputStateData
{
	public float floatParam1;
	public float floatParam2;
	public float floatParam3;
	public int intParam1;
	public bool boolParam1;
	public bool triggerParam1;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
[UpdateBefore(typeof(RukhankaAnimationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class PlayerControllerSystem: SystemBase
{
	static readonly string floatParam1Name = "FloatParam1";
	static FastAnimatorParameter floatParam1  = new (floatParam1Name);
	static readonly string floatParam2Name = "FloatParam2";
	static FastAnimatorParameter floatParam2  = new (floatParam2Name);
	static readonly string floatParam3Name = "FloatParam3";
	static FastAnimatorParameter floatParam3  = new (floatParam3Name);
	static readonly string intParam1Name = "IntParam1";
	static FastAnimatorParameter intParam1  = new (intParam1Name);
	static readonly string boolParam1Name = "BoolParam1";
	static FastAnimatorParameter boolParam1 = new (boolParam1Name);
	static readonly string triggerParam1Name = "TriggerParam1";
	static FastAnimatorParameter triggerParam1 = new (triggerParam1Name);

	Animator[] managedAnimators;

	bool trigger1Value;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	partial struct ProcessInputJob: IJobEntity
	{
		public InputStateData inputData;

		public FastAnimatorParameter floatParam3P;
		public FastAnimatorParameter floatParam2P;
		public FastAnimatorParameter floatParam1P;
		public FastAnimatorParameter intParam1P;
		public FastAnimatorParameter boolParam1P;
		public FastAnimatorParameter triggerParam1P;

		void Execute(AnimatorParametersAspect paramAspect)
		{
			if (paramAspect.HasParameter(floatParam1P))
				paramAspect.SetParameterValue(floatParam1P, inputData.floatParam1);
			if (paramAspect.HasParameter(floatParam2P))
				paramAspect.SetParameterValue(floatParam2P, inputData.floatParam2);
			if (paramAspect.HasParameter(floatParam3P))
				paramAspect.SetParameterValue(floatParam3P, inputData.floatParam2);
			if (paramAspect.HasParameter(intParam1P))
				paramAspect.SetParameterValue(intParam1P, inputData.intParam1);
			if (paramAspect.HasParameter(boolParam1P))
				paramAspect.SetParameterValue(boolParam1P, inputData.boolParam1);

			if (inputData.triggerParam1)
				paramAspect.SetParameterValue(triggerParam1P, inputData.triggerParam1);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnStartRunning()
	{
		base.OnStartRunning();

		var setTriggerBtn = GameObject.Find(triggerParam1Name);
		if (setTriggerBtn != null)
		{
			setTriggerBtn.GetComponent<Button>().onClick.AddListener(SetTriggerButtonClick);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void SetTriggerButtonClick()
	{
		trigger1Value = true;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	InputStateData GatherInput()
	{
		var floatParam1Slider = GameObject.Find(floatParam1Name);
		var floatParam1Value = 0.0f;
		if (floatParam1Slider != null)
			floatParam1Value = floatParam1Slider.GetComponent<Slider>().value;

		var floatParam2Value = 0.0f;
		var floatParam2Slider = GameObject.Find(floatParam2Name);
		if (floatParam2Slider != null)
			floatParam2Value = floatParam2Slider.GetComponent<Slider>().value;

		var floatParam3Slider = GameObject.Find(floatParam3Name);
		var floatParam3Value = 0.0f;
		if (floatParam3Slider != null)
			floatParam3Value = floatParam3Slider.GetComponent<Slider>().value;

		int intParam1Value = 0;
		var intSlider1 = GameObject.Find(intParam1Name);
		if (intSlider1 != null)
			intParam1Value = (int)intSlider1.GetComponent<Slider>().value;

		bool boolParam1Value = false;
		var boolToggle = GameObject.Find(boolParam1Name);
		if (boolToggle != null)
			boolParam1Value = boolToggle.GetComponent<Toggle>().isOn;

		var rv = new InputStateData();
		if (math.any(new float2(floatParam1Value, floatParam2Value)))
		{
			rv.floatParam1 = floatParam1Value;
			rv.floatParam2 = floatParam2Value;
		}
		rv.floatParam3 = floatParam3Value;
		rv.intParam1 = intParam1Value;
		rv.triggerParam1 = trigger1Value;
		rv.boolParam1 = boolParam1Value;

		trigger1Value = false;

		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ControlUnityManagedAnimatorControllers(in InputStateData isd)
	{
		if (managedAnimators == null)
			managedAnimators = GameObject.FindObjectsByType<Animator>(FindObjectsSortMode.None);

		for (int i = 0; i < managedAnimators.Length; ++i)
		{
			var a = managedAnimators[i];
			if (a.runtimeAnimatorController == null)
				continue;

			if (Array.FindIndex(a.parameters, x => x.name == boolParam1Name) >= 0)
				a.SetBool(boolParam1Name, isd.boolParam1);

			if (Array.FindIndex(a.parameters, x => x.name == triggerParam1Name) >= 0 && isd.triggerParam1)
				a.SetTrigger(triggerParam1Name);

			if (Array.FindIndex(a.parameters, x => x.name == floatParam1Name) >= 0)
				a.SetFloat(floatParam1Name, isd.floatParam1);

			if (Array.FindIndex(a.parameters, x => x.name == floatParam2Name) >= 0)
				a.SetFloat(floatParam2Name, isd.floatParam2);

			if (Array.FindIndex(a.parameters, x => x.name == floatParam3Name) >= 0)
				a.SetFloat(floatParam3Name, isd.floatParam3);

			if (Array.FindIndex(a.parameters, x => x.name == intParam1Name) >= 0)
				a.SetInteger(intParam1Name, isd.intParam1);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
		var inputData = GatherInput();
		ControlUnityManagedAnimatorControllers(inputData);

		var processInputJob = new ProcessInputJob()
		{
			inputData = inputData,
			floatParam1P = floatParam1,
			floatParam2P = floatParam2,
			floatParam3P = floatParam3,
			intParam1P = intParam1,
			triggerParam1P = triggerParam1,
			boolParam1P = boolParam1
		};

		Dependency = processInputJob.ScheduleParallel(Dependency);
	}
}
}

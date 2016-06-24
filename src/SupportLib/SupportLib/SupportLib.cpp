// This is the main DLL file.
#include "stdafx.h"
#include <malloc.h>
#include <stdlib.h>
#include <stdio.h>
#include "stdmpi.h"
#include "firmware.h"
#include "kollmorgen_s200.h"
#include "SupportLib.h"

static float comm_offset[16] = {
	-157.5,
	-135,
	180,
	157.5,
	-90,
	-112.5,
	-67.5,
	-45,
	90,
	67.5,
	112.5,
	135,
	22.5,
	45,
	0,
	-22.5,
};

static MPI_BOOL MPI_DECL2 eventServiceErrorHandler(MPIControlEventServiceErrorParams *params)
{
	return FALSE;
}

uint32_t readParameter(MPISqNode sqNode, long driveNumber, long readParam, MPIDriveMapParamType paramType, float *readValue)
{
	uint32_t				returnValue = 0;
#if !defined __FAKEOUT__
	MPIDriveMapParamValue value;
	returnValue = mpiSqNodeDriveParamGet(sqNode, driveNumber, readParam, paramType, &value);

	mpiPlatformSleep(100);

	if (returnValue == MPIMessageOK)
	{
		if (paramType == MPIDriveMapParamTypeSIGNED32)
		{
			*readValue = (float)(value.signed32);
		}
		else
		{
			*readValue = (value.single);
		}
	}
#endif
	return returnValue;
}

uint32_t writeParameter(MPISqNode sqNode, long driveNumber, long writeParam, MPIDriveMapParamType paramType, float parameterValue)
{
	uint32_t				returnValue = 0;
#if !defined __FAKEOUT__
	MPIDriveMapParamValue value;

	if (paramType == MPIDriveMapParamTypeSINGLE)
	{
		value.single = parameterValue;
	}

	if (paramType == MPIDriveMapParamTypeSIGNED32)
	{
		value.signed32 = (long)parameterValue;
	}

	returnValue = mpiSqNodeDriveParamSet(sqNode, driveNumber, writeParam, paramType, &value);

	// To make sure it gets written before we move on
	mpiPlatformSleep(100);
#endif
	return returnValue;
}

namespace SupportLib {
	public enum class MotionState
	{
		Done = 0,
		Moving = 1,
		Error = 2,
	};

	public enum class CaptureState
	{
		Idle = 0,
		Armed = 1,
		Captured = 2,
		Clear = 3,
	};

	public enum class Gain
	{
		Kp,
		Ki,
		Kd,
		Kpff,
		Kvff,
		Kaff,
		Kfff,
		IMaxMoving,
		IMaxRest,
		DRate,
		OutputLimit,
		OutputLimitLow,
		OutputLimitHigh,
		OutputOffset,
	};

	public ref class Controller
	{

	private:
		MPIControl					*control;
		MPIPlatform					*platform;
		MPISynqNet					*synqnet;
		MPIGeometricPath 			*path;
		MPIMotor					*motor;
		MPIMotion					*motion;
		MPIAxis						*axis;
		MPIFilter					*filter;
		MPICapture					*capture;
		MPIUserLimit				*limit;
		MPISqNode					*sqNode;
		int							elementCount;
		double						*path_x;
		double						*path_y;
		double						*path_z;
		int							count;
		uint32_t					robot_index;
		uint32_t					first_axis;
		bool						multi_axis;
		bool						engraver_config;
		MFWAxisData					*(*AxisData);
		MFWMotionSupervisorData		*(*MotionData);
		MFWMotionSupervisorConfig	*(*MotionConfig);
		int32_t						axes;
		int32_t						nodes;
	public:

		uint32_t Initialize(int32_t index, bool engraver)
		{
			uint32_t			returnValue = MPIMessageOK;
			robot_index = index;
			first_axis = robot_index  * ROBOT_AXES;
			engraver_config = engraver;
			axes = engraver_config ? ENGRAVER_AXES : ROBOT_AXES;
			nodes = engraver_config ? 1 : ROBOT_AXES;

#if !defined __FAKEOUT__
			MPIControlType      controlType;
			MPIControlAddress   controlAddress;
			int32_t				motor_number;
			int32_t				node_number;

			controlType = MPIControlTypeDEVICE;
			controlAddress.number = 0;

			control = (MPIControl *)malloc(sizeof(MPIControl));
			platform = (MPIPlatform *)malloc(sizeof(MPIPlatform));
			synqnet = (MPISynqNet *)malloc(sizeof(MPISynqNet));
			path = (MPIGeometricPath *)malloc(sizeof(MPIGeometricPath));
			motor = (MPIMotor *)malloc(axes * sizeof(MPIMotor));
			motion = (MPIMotion *)malloc(axes * sizeof(MPIMotion));
			axis = (MPIAxis *)malloc(axes * sizeof(MPIAxis));
			filter = (MPIFilter *)malloc(axes * sizeof(MPIFilter));
			capture = (MPICapture *)malloc(axes * sizeof(MPICapture));
			limit = (MPIUserLimit *)malloc(axes * sizeof(MPIUserLimit));
			sqNode = (MPISqNode *)malloc(nodes * sizeof(MPISqNode));
			AxisData = (MFWAxisData **)malloc(axes * sizeof(MFWAxisData *));
			MotionData = (MFWMotionSupervisorData **)malloc(axes * sizeof(MFWMotionSupervisorData *));
			MotionConfig = (MFWMotionSupervisorConfig **)malloc(axes * sizeof(MFWMotionSupervisorConfig *));

			returnValue = mpiControlCreate(control, controlType, &controlAddress);
			CHECK_RETURN(returnValue);

			returnValue = mpiControlPlatform(*control, platform);
			CHECK_RETURN(returnValue);

			returnValue = mpiSynqNetCreate(synqnet, *control, 0);
			CHECK_RETURN(returnValue);

			returnValue = mpiGeometricPathCreate(path);
			CHECK_RETURN(returnValue);

			for (motor_number = 0; motor_number < axes; motor_number++)
			{
				/* Create motor object */
				returnValue = mpiMotorCreate(&motor[motor_number], *control, motor_number + first_axis);
				CHECK_RETURN(returnValue);

				returnValue = mpiFilterCreate(&filter[motor_number], *control, motor_number + first_axis);
				CHECK_RETURN(returnValue);

				returnValue = mpiAxisCreate(&axis[motor_number], *control, motor_number + first_axis);
				CHECK_RETURN(returnValue);

				returnValue = mpiMotionCreate(&motion[motor_number], *control, motor_number + first_axis);
				CHECK_RETURN(returnValue);

				returnValue = mpiCaptureCreate(&capture[motor_number], *control, motor_number + first_axis);
				CHECK_RETURN(returnValue);

				returnValue = mpiUserLimitCreate(&limit[motor_number], *control, motor_number + first_axis);
				CHECK_RETURN(returnValue);

				returnValue = mpiMotionMemory(motion[motor_number], NULL, &MotionData[motor_number], &MotionConfig[motor_number]);
				CHECK_RETURN(returnValue);

				returnValue = mpiAxisMemory(axis[motor_number], NULL, &AxisData[motor_number], NULL);
				CHECK_RETURN(returnValue);
			}

			for (node_number = 0; node_number < axes; node_number++)
			{
				returnValue = mpiSqNodeCreate(&sqNode[node_number], *synqnet, node_number + first_axis);
				CHECK_RETURN(returnValue);
			}

			returnValue = ConfigSingleAxis();
			CHECK_RETURN(returnValue);

			path_x = NULL;
			path_y = NULL;
			path_z = NULL;
			count = 0;

#endif
			return returnValue;
		}

		uint32_t Delete()
		{
			uint32_t	returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			int32_t	motor_number;
			int32_t	node_number;
			for (motor_number = 0; motor_number < axes; motor_number++)
			{
				returnValue = mpiCaptureDelete(capture[motor_number]);
				CHECK_RETURN(returnValue);

				returnValue = mpiUserLimitDelete(limit[motor_number]);
				CHECK_RETURN(returnValue);

				returnValue = mpiSqNodeDelete(sqNode[motor_number]);
				CHECK_RETURN(returnValue);

				returnValue = mpiMotionDelete(motion[motor_number]);
				CHECK_RETURN(returnValue);

				returnValue = mpiAxisDelete(axis[motor_number]);
				CHECK_RETURN(returnValue);

				returnValue = mpiFilterDelete(filter[motor_number]);
				CHECK_RETURN(returnValue);

				returnValue = mpiMotorDelete(motor[motor_number]);
				CHECK_RETURN(returnValue);

			}

			for (node_number = 0; node_number < nodes; node_number++)
			{
				returnValue = mpiSqNodeDelete(sqNode[node_number]);
				CHECK_RETURN(returnValue);
			}
			free(motor);
			free(filter);
			free(axis);
			free(motion);
			free(sqNode);
			free(capture);
			free(limit);

			mpiGeometricPathDelete(*path);
			free(path);

			if (path_x)
			{
				free(path_x);
			}
			if (path_y)
			{
				free(path_y);
			}
			if (path_z)
			{
				free(path_z);
			}

			returnValue = mpiSynqNetDelete(*synqnet);
			free(synqnet);
			CHECK_RETURN(returnValue);

			free(platform);
			returnValue = mpiControlDelete(*control);
			free(control);
			free(AxisData);
			free(MotionData);
			free(MotionConfig);
#endif
			return returnValue;
		}

		uint32_t SetGain(int32_t axis, Gain gain, double value)
		{
			uint32_t returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIFilterGain filter_gain;

			returnValue = mpiFilterGainGet(filter[axis], 0, &filter_gain);
			CHECK_RETURN(returnValue);

			switch (gain)
			{
			case Gain::Kp:
				filter_gain.PID.gain.proportional = value;
				break;
			case Gain::Ki:
				filter_gain.PID.gain.integral = value;
				break;
			case Gain::Kd:
				filter_gain.PID.gain.derivative = value;
				break;
			case Gain::Kpff:
				filter_gain.PID.feedForward.position = value;
				break;
			case Gain::Kvff:
				filter_gain.PID.feedForward.velocity = value;
				break;
			case Gain::Kaff:
				filter_gain.PID.feedForward.acceleration = value;
				break;
			case Gain::Kfff:
				filter_gain.PID.feedForward.friction = value;
				break;
			case Gain::DRate:
				filter_gain.PID.dRate = value;
				break;
			case Gain::IMaxMoving:
				filter_gain.PID.integrationMax.moving = value;
				break;
			case Gain::IMaxRest:
				filter_gain.PID.integrationMax.rest = value;
				break;
			case Gain::OutputLimit:
				filter_gain.PID.output.limitLow = -value;
				filter_gain.PID.output.limitHigh = value;
				break;
			case Gain::OutputLimitLow:
				filter_gain.PID.output.limitLow = value;
				break;
			case Gain::OutputLimitHigh:
				filter_gain.PID.output.limitHigh = value;
				break;
			case Gain::OutputOffset:
				filter_gain.PID.output.offset = value;
				break;
			default:
				break;
			}
			returnValue = mpiFilterGainSet(filter[axis], 0, &filter_gain);
#endif
			return returnValue;
		}

		uint32_t SetCommutationOffsets()
		{
			uint32_t	returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			uint32_t C1;
			uint32_t C2;
			uint32_t C4;
			uint32_t C8;
			uint32_t motor_number;
			uint32_t index;
			float offset;

			for (motor_number = 0; motor_number < ROBOT_AXES; motor_number++)
			{
				returnValue = mpiMotorGeneralIn(motor[motor_number], 4, 1, &C1);
				CHECK_RETURN(returnValue);
				returnValue = mpiMotorGeneralIn(motor[motor_number], 5, 1, &C2);
				CHECK_RETURN(returnValue);
				returnValue = mpiMotorGeneralIn(motor[motor_number], 6, 1, &C4);
				CHECK_RETURN(returnValue);
				returnValue = mpiMotorDedicatedIn(motor[motor_number], 2, 1, &C8);
				CHECK_RETURN(returnValue);
				index = C1 + 2 * C2 + 4 * C4 + 8 * C8;
				offset = comm_offset[index];
				returnValue = writeParameter(sqNode[motor_number], 0, S200ParamCOMM_OFF, MPIDriveMapParamTypeSINGLE, offset);
				CHECK_RETURN(returnValue);
			}
#endif
			return returnValue;
		}

		uint32_t IntializeDefaults()
		{
			uint32_t	returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIMotorConfig	motorConfig;
			int32_t			motor_number;
			MFWData			*Xmp;
			returnValue = mpiControlMemory(*control, &Xmp, NULL);

			for (motor_number = 0; motor_number < axes; motor_number++)
			{
				/* Configure userFaults for each node to monitor a controller input bit. */
				MPISqNodeConfig     nodeConfig;
				returnValue = mpiSqNodeConfigGet(sqNode[motor_number], &nodeConfig);
				CHECK_RETURN(returnValue);

				/* Trigger on Controller's User Input #0 */
				CHECK_RETURN(returnValue);
				nodeConfig.userFault.addr = &Xmp->SystemData.IO.Input[0].IO;
				nodeConfig.userFault.mask = (uint32_t)MPIControlInputXESTOP;
				nodeConfig.userFault.pattern = (uint32_t)MPIControlInputXESTOP;

				returnValue = mpiSqNodeConfigSet(sqNode[motor_number], &nodeConfig);
				CHECK_RETURN(returnValue);

				/* Configure the motors associated with each sqNode to generate
				an action when the userFault is activated.
				*/
				returnValue = mpiMotorConfigGet(motor[motor_number], &motorConfig);
				CHECK_RETURN(returnValue);

				/* ABORT action when userFault is triggered */
				motorConfig.userFaultAction = MPIActionE_STOP;
				/* disable HW and SW limits */
				motorConfig.limit[MPIMotorLimitTypeHW_POS].action = MPIActionNONE;
				motorConfig.limit[MPIMotorLimitTypeHW_NEG].action = MPIActionNONE;
				motorConfig.limit[MPIMotorLimitTypeSW_POS].action = MPIActionNONE;
				motorConfig.limit[MPIMotorLimitTypeSW_NEG].action = MPIActionNONE;

				returnValue = mpiMotorConfigSet(motor[motor_number], &motorConfig);
				CHECK_RETURN(returnValue);
			}

			for (motor_number = 0; motor_number < axes; motor_number++)
			{
				MPICaptureConfig     capture_config;
				returnValue = mpiCaptureConfigReset(capture[motor_number]);
				CHECK_RETURN(returnValue);
				returnValue = mpiCaptureConfigGet(capture[motor_number], &capture_config);
				CHECK_RETURN(returnValue);

				capture_config.engineNumber = 0;
				capture_config.feedbackInput = MPIMotorFeedbackInputPRIMARY;
				capture_config.feedbackMotorNumber = motor_number + first_axis;
				capture_config.mode = MPICaptureModeSINGLE_SHOT;
				capture_config.type = MPICaptureTypeTIME;
				capture_config.triggerLogic = MPICaptureTriggerLogicIGNORE_PRECONDITION;
				capture_config.triggerType = MPICaptureTriggerTypeMOTOR;
				capture_config.motor.motorNumber = motor_number + first_axis;
				capture_config.motor.trigger.edge = MPICaptureEdgeRISING;
				capture_config.motor.trigger.inputFilter = MPICaptureInputFilterSLOW;
				capture_config.motor.trigger.ioType = MPICaptureIoTypeMOTOR_DEDICATED;
				capture_config.motor.trigger.dedicatedIn = MPIMotorDedicatedInHOME;

				returnValue = mpiCaptureConfigSet(capture[motor_number], &capture_config);
				CHECK_RETURN(returnValue);
			}

			returnValue = mpiMotorConfigGet(motor[0], &motorConfig);
			CHECK_RETURN(returnValue);
			motorConfig.io[MPIMotorGeneralIo2].type = MPIMotorIoTypeOUTPUT;
			returnValue = mpiMotorConfigSet(motor[0], &motorConfig);
			CHECK_RETURN(returnValue);

			MPIControlEventServiceErrorConfig serviceErrorConfig;
			serviceErrorConfig.errorHandler = eventServiceErrorHandler;
			serviceErrorConfig.errorThreshold = 1;
			returnValue = mpiControlEventServiceStart(*control, MPIThreadPriorityHIGHEST, MPIWaitFOREVER, &serviceErrorConfig);

#endif
			return returnValue;
		}

		uint32_t  StartPath(double x, double y, double velocity, double acceleration)
		{
			uint32_t				returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIGeometricPathParams	pathParams;

			if (path)
			{
				mpiGeometricPathDelete(*path);
			}

			returnValue = mpiGeometricPathCreate(path);
			CHECK_RETURN(returnValue);

			returnValue = mpiGeometricPathParamsGet(*path, &pathParams);
			CHECK_RETURN(returnValue);

			pathParams.dimension = 2;
			MPIGeometricPathPointX(pathParams.start) = x * INTERNAL_SCALE;
			MPIGeometricPathPointY(pathParams.start) = y * INTERNAL_SCALE;
			pathParams.velocity = velocity * INTERNAL_SCALE;
			pathParams.acceleration = acceleration * INTERNAL_SCALE;
			pathParams.deceleration = acceleration * INTERNAL_SCALE;
			pathParams.interpolation = MPIGeometricPathInterpolationTypeBSPLINE;
			pathParams.timeSlice = 0.050;
			elementCount = 0;
			count = 0;

			returnValue = mpiGeometricPathParamsSet(*path, &pathParams);
#endif
			return returnValue;
		}

		uint32_t  StartPath(double x, double y, double z, double velocity, double acceleration)
		{
			uint32_t				returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIGeometricPathParams		pathParams;

			if (path)
			{
				mpiGeometricPathDelete(*path);
			}

			returnValue = mpiGeometricPathCreate(path);
			CHECK_RETURN(returnValue);

			returnValue = mpiGeometricPathParamsGet(*path, &pathParams);
			CHECK_RETURN(returnValue);

			pathParams.dimension = 3;
			MPIGeometricPathPointX(pathParams.start) = x * INTERNAL_SCALE;
			MPIGeometricPathPointY(pathParams.start) = y * INTERNAL_SCALE;
			MPIGeometricPathPointZ(pathParams.start) = z * INTERNAL_SCALE;
			pathParams.velocity = velocity * INTERNAL_SCALE;
			pathParams.acceleration = acceleration * INTERNAL_SCALE;
			pathParams.deceleration = acceleration * INTERNAL_SCALE;
			pathParams.interpolation = MPIGeometricPathInterpolationTypeBSPLINE;
			pathParams.timeSlice = 0.050;
			elementCount = 0;
			count = 0;

			returnValue = mpiGeometricPathParamsSet(*path, &pathParams);
#endif
			return returnValue;
		}

		uint32_t FinishPath(void)
		{
			uint32_t				returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIGeometricPathParams	pathParams;

			if (path_x)
			{
				free(path_x);
				path_x = NULL;
			}
			if (path_y)
			{
				free(path_y);
				path_y = NULL;
			}
			if (path_z)
			{
				free(path_z);
				path_z = NULL;
			}

			if (elementCount)
			{
				int32_t				index;
				MPIMotionPTPoint	*point;
				int32_t				pointCount;

				elementCount = 0;

				returnValue = mpiGeometricPathParamsGet(*path, &pathParams);
				CHECK_RETURN(returnValue);

				returnValue = mpiGeometricPathToPTPoints(*path, &point, &pointCount, NULL);
				if (returnValue == MPIMessageOK)
				{
					count = pointCount;
					path_x = (double *)malloc(pointCount * sizeof(double));
					path_y = (double *)malloc(pointCount * sizeof(double));
					switch (pathParams.dimension)
					{
					case 2:
						for (index = 0; index < pointCount; index++)
						{
							path_x[index] = point[index].position[0];
							path_y[index] = point[index].position[1];
						}
						break;
					case 3:
						path_z = (double *)malloc(pointCount * sizeof(double));
						for (index = 0; index < pointCount; index++)
						{
							path_x[index] = point[index].position[0];
							path_y[index] = point[index].position[1];
							path_z[index] = point[index].position[2];
						}
						break;
					default:
						count = 0;
						break;
					}
				}
			}
#endif
			return count;
		}

		int	AddPoint(double x, double y)
		{
			uint32_t				returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIGeometricPathElement	element;

			element.type = MPIGeometricPathElementTypeLINE;
			MPIGeometricPathPointX(element.params.line.point) = x * INTERNAL_SCALE;
			MPIGeometricPathPointY(element.params.line.point) = y * INTERNAL_SCALE;

			returnValue = mpiGeometricPathSimpleAppend(*path, &element);
			elementCount++;
#endif
			return returnValue;
		}

		int	AddPoint(double x, double y, double z)
		{
			uint32_t				returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIGeometricPathElement	element;

			element.type = MPIGeometricPathElementTypeLINE;
			MPIGeometricPathPointX(element.params.line.point) = x * INTERNAL_SCALE;
			MPIGeometricPathPointY(element.params.line.point) = y * INTERNAL_SCALE;
			MPIGeometricPathPointZ(element.params.line.point) = z * INTERNAL_SCALE;

			returnValue = mpiGeometricPathSimpleAppend(*path, &element);
			elementCount++;
#endif
			return returnValue;
		}

		int	AddArc(double x, double y, double angle)
		{
			uint32_t				returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIGeometricPathElement	element;

			element.type = MPIGeometricPathElementTypeARC_CENTER;
			MPIGeometricPathPointX(element.params.arcCenter.center) = x * INTERNAL_SCALE;
			MPIGeometricPathPointY(element.params.arcCenter.center) = y * INTERNAL_SCALE;
			element.params.arcCenter.angle = angle;

			returnValue = mpiGeometricPathSimpleAppend(*path, &element);
			elementCount++;
#endif
			return returnValue;
		}
		double PathX(int index)
		{
			if (index < count)
			{
				return path_x[index] / INTERNAL_SCALE;
			}
			return 0.0;
		}
		double PathY(int index)
		{
			if (index < count)
			{
				return path_y[index] / INTERNAL_SCALE;
			}
			return 0.0;
		}

		double PathZ(int index)
		{
			if (index < count)
			{
				return path_z[index] / INTERNAL_SCALE;
			}
			return 0.0;
		}
		double ActualPosition(int32_t axis_number)
		{
#if !defined __FAKEOUT__
			double position;
			uint32_t returnValue = mpiAxisActualPositionGet(axis[axis_number], &position);
			if (returnValue == MPIMessageOK)
			{
				return position;
			}
			else
#endif
			{
				return 0.0;
			}
		}
		double CommandPosition(int32_t axis_number)
		{
#if !defined __FAKEOUT__
			double position;
			uint32_t returnValue = mpiAxisCommandPositionGet(axis[axis_number], &position);
			if (returnValue == MPIMessageOK)
			{
				return position;
			}
			else
#endif
			{
				return 0.0;
			}
		}

		uint32_t MotorOutSet(int32_t motor_number, int32_t output_number, bool value)
		{
			uint32_t returnValue = mpiMotorGeneralOutSet(motor[motor_number], output_number, 1, value ? 1 : 0, FALSE);
			return returnValue;
		}

		uint32_t DigitalOutSet(int32_t output_number, bool value)
		{
			uint32_t returnValue = mpiControlDigitalOutSet(*control, output_number, 1, value ? 1 : 0, FALSE);
			return returnValue;
		}

		MotionState GetMotionState(int32_t axis)
		{
			uint32_t returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIMotionStatus status;

			returnValue = mpiMotionStatus(motion[axis], &status);
			if (returnValue == MPIMessageOK)
			{
				switch (status.state)
				{
				default:
				case MPIStateIDLE:
				case MPIStateSTOPPED:
					return MotionState::Done;
				case MPIStateMOVING:
				case MPIStateSTOPPING:
					return MotionState::Moving;
				case MPIStateSTOPPING_ERROR:
				case MPIStateERROR:
					return MotionState::Error;
				}
			}

#endif
			return MotionState::Done;
		}

		uint32_t InitializeAxis(int32_t axis_number, double error_limit, double settling_distance, double stop_time, double estop_time)
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIMotorConfig	motorConfig;
			returnValue = mpiMotorConfigGet(motor[axis_number], &motorConfig);
			CHECK_RETURN(returnValue);

			motorConfig.limit[MPIMotorLimitTypeERROR].trigger.error = error_limit;

			returnValue = mpiMotorConfigSet(motor[axis_number], &motorConfig);
			CHECK_RETURN(returnValue);

			MPIAxisConfig	axisConfig;
			returnValue = mpiAxisConfigGet(axis[axis_number], &axisConfig);
			CHECK_RETURN(returnValue);

			axisConfig.settle.tolerance.distance = settling_distance;

			returnValue = mpiAxisConfigSet(axis[axis_number], &axisConfig);
			CHECK_RETURN(returnValue);

			MPIMotionConfig	motionConfig;
			returnValue = mpiMotionConfigGet(motion[axis_number], &motionConfig);
			CHECK_RETURN(returnValue);

			motionConfig.decelTime.stop = stop_time;
			motionConfig.decelTime.eStop = estop_time;

			returnValue = mpiMotionConfigSet(motion[axis_number], &motionConfig);
#endif
			return returnValue;
		}

		uint32_t ConfigSingleAxis()
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			if (multi_axis)
			{
				MPIMotionAxisMap axis_map;
				axis_map.count = 1;
				returnValue = mpiAxisNumber(axis[0], &axis_map.number[0]);
				CHECK_RETURN(returnValue);
				returnValue = mpiMotionAxisMapSet(motion[0], &axis_map);
				multi_axis = false;
			}
#endif
			return returnValue;
		}

		uint32_t ConfigMultiAxis()
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			if (!multi_axis)
			{
				int axis_number;
				MPIMotionAxisMap axis_map;
				axis_map.count = axes;
				for (axis_number = 0; axis_number < axes; axis_number++)
				{
					returnValue = mpiAxisNumber(axis[axis_number], &axis_map.number[axis_number]);
					CHECK_RETURN(returnValue);
				}
				returnValue = mpiMotionAxisMapSet(motion[0], &axis_map);
				CHECK_RETURN(returnValue);
				multi_axis = true;
			}
#endif
			return returnValue;
		}

		uint32_t Stop(int32_t axis_number)
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			if (multi_axis)
			{
				returnValue = mpiMotionAction(motion[0], MPIActionSTOP);
			}
			else
			{
				returnValue = mpiMotionAction(motion[axis_number], MPIActionSTOP);
				CHECK_RETURN(returnValue);
			}
#endif
			return returnValue;
		}

		uint32_t EStop(int32_t axis_number)
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			if (multi_axis)
			{
				returnValue = mpiMotionAction(motion[0], MPIActionE_STOP);
			}
			else
			{
				returnValue = mpiMotionAction(motion[axis_number], MPIActionE_STOP);
				CHECK_RETURN(returnValue);
			}
#endif
			return returnValue;
		}

		uint32_t FaultsClear(int32_t axis_number)
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			if (multi_axis)
			{
				returnValue = mpiMotionAction(motion[0], MPIActionRESET);
			}
			else
			{
				returnValue = mpiSqNodeStatusClear(sqNode[axis_number]);
				CHECK_RETURN(returnValue);
				returnValue = mpiMotionAction(motion[axis_number], MPIActionRESET);
				CHECK_RETURN(returnValue);
			}
#endif
			return returnValue;
		}

		uint32_t Enable(int32_t axis_number, bool enable)
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			int32_t amp_enable = enable ? 1 : 0;
			returnValue = mpiMotorAmpEnableSet(motor[axis_number], amp_enable);
#endif
			return returnValue;
		}

		uint32_t SCurve(int32_t axis_number, double target, double speed, double acceleration, double deceleration)
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			returnValue = mpiMotionSimpleSCurveJerkPercentMove(motion[axis_number], target, speed, acceleration, deceleration, 66.0);
#endif
			return returnValue;
		}

		uint32_t Velocity(int32_t axis_number, double speed, double acceleration)
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIMotionVelocityAttributes	attr;
			attr.behavior = MPIMotionVelocityBehaviorTypeMODIFY;
			returnValue = mpiMotionVelocityJerkPercentMove(motion[axis_number], speed, acceleration, 0.0, MPIMotionVelocityAttrMaskBEHAVIOR, &attr);
#endif
			return returnValue;
		}

		uint32_t SCurve(int32_t axis_number, array< double >^ target, double speed, double acceleration, double deceleration)
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			double x[MAX_AXES];
			int i;
			for (i = 0; i < axes; i++)
			{
				x[i] = target[i];
			}
			returnValue = mpiMotionSimpleSCurveJerkPercentCartesianMove(motion[axis_number], x, speed, acceleration, deceleration, 66.0);
#endif
			return returnValue;
		}

		uint32_t PVT(int32_t axis_number, int32_t point_count, array< double >^ x, array< double >^ v, array< double >^ t)
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIMotionPVTPoint *points = (MPIMotionPVTPoint *)malloc(point_count * sizeof(MPIMotionPVTPoint));
			int i;
			for (i = 0; i < point_count; i++)
			{
				points[i].position[0] = x[i];
				points[i].velocity[0] = v[i];
				points[i].timeDelta = t[i];
			}
			returnValue = mpiMotionSimplePVTMove(motion[axis_number], point_count, points);
			free(points);
#endif
			return returnValue;
		}

		uint32_t BSpline(int32_t axis_number, int32_t point_count, array< double >^ x, array< double >^ t)
		{
			uint32_t		returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIMotionPTPoint *points = (MPIMotionPTPoint *)malloc(point_count * sizeof(MPIMotionPTPoint));
			int i;
			for (i = 0; i < point_count; i++)
			{
				points[i].position[0] = x[i];
				points[i].timeDelta = t[i];
			}
			returnValue = mpiMotionSimpleBSplineMove(motion[axis_number], point_count, points);
			free(points);
#endif
			return returnValue;
		}

		double GetFeedrate(int32_t axis_number)
		{
			uint32_t	returnValue = MPIMessageOK;
			double		feedrate = 1.0;
#if !defined __FAKEOUT__
			returnValue = mpiMotionMemoryGet(motion[axis_number], &feedrate, (double *)&MotionData[axis_number]->FeedRate.Current, sizeof(feedrate));
			if (returnValue == MPIMessageOK)
			{
				returnValue = mpiPlatformWord64Orient(*platform, (int64_t *)&feedrate, (int64_t *)&feedrate);
			}
#endif
			return feedrate;
		}

		uint32_t SetFeedrate(int32_t axis_number, double feedrate)
		{
			uint32_t	returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			returnValue = mpiPlatformWord64Orient(*platform, (int64_t *)&feedrate, (int64_t *)&feedrate);
			if (returnValue == MPIMessageOK)
			{
				returnValue = mpiMotionMemorySet(motion[axis_number], (double *)&MotionConfig[axis_number]->FeedRate.Target, &feedrate, sizeof(feedrate));
			}
#endif
			return returnValue;
		}

		double GetMoveTime(int32_t axis_number)
		{
			uint32_t	returnValue = MPIMessageOK;
			double		move_time = 0.0;
#if !defined __FAKEOUT__
			returnValue = mpiAxisMemoryGet(axis[axis_number], &move_time, (double *)&AxisData[axis_number]->Metric.ElapsedTime, sizeof(move_time));
			if (returnValue == MPIMessageOK)
			{
				if (returnValue == MPIMessageOK)
				{
					returnValue = mpiPlatformWord64Orient(*platform, (int64_t *)&move_time, (int64_t *)&move_time);
				}
			}
#endif
			return move_time;
		}

		uint32_t ConfigureUserLimit(int32_t axis_number, double velocity, double error_limit)
		{
			uint32_t	returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			MPIUserLimitConfig config;
			returnValue = mpiUserLimitConfigDefault(&config);
			CHECK_RETURN(returnValue);
			config.trigger.type = MPIUserLimitTriggerTypeSINGLE_CONDITION;
			config.trigger.condition[0].type = MPIUserLimitConditionTypeAXIS_POSITION_ERROR;
			config.trigger.condition[0].data.axisPositionError.axisNumber = axis_number + first_axis;
			config.trigger.condition[0].data.axisPositionError.positionError = velocity > 0.0 ? error_limit : -error_limit;
			config.trigger.condition[0].data.axisPositionError.logic = velocity > 0.0 ? MPIUserLimitLogicGT : MPIUserLimitLogicLT;
			config.action = MPIActionSTOP;
			config.actionAxis = axis_number + first_axis;
			returnValue = mpiUserLimitConfigSet(limit[axis_number], &config);
#endif
			return returnValue;
		}

		uint32_t EnableUserLimit(int32_t axis_number, bool enable)
		{
			uint32_t	returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			returnValue = mpiUserLimitEnableSet(limit[axis_number], enable ? TRUE : FALSE);
#endif
			return returnValue;
		}

		uint32_t CaptureArm(int32_t axis_number, bool arm)
		{
			uint32_t	returnValue = MPIMessageOK;
#if !defined __FAKEOUT__
			returnValue = mpiCaptureArm(capture[axis_number], arm ? TRUE : FALSE);
#endif
			return returnValue;
		}

		CaptureState  CaptureState(int32_t axis_number)
		{
			SupportLib::CaptureState state = SupportLib::CaptureState::Idle;
#if !defined __FAKEOUT__
			MPICaptureStatus status;
			uint32_t returnValue = mpiCaptureStatus(capture[axis_number], &status);
			if (returnValue == MPIMessageOK)
			{
				switch (status.state)
				{
				default:
				case MPICaptureStateIDLE:
					state = SupportLib::CaptureState::Idle;
					break;
				case MPICaptureStateARMED:
					state = SupportLib::CaptureState::Armed;
					break;
				case MPICaptureStateCAPTURED:
					state = SupportLib::CaptureState::Captured;
					break;
				case MPICaptureStateCLEAR:
					state = SupportLib::CaptureState::Clear;
					break;
				}
			}
#endif
			return state;
		}

		double  CapturePosition(int32_t axis_number)
		{
			double position = 0.0;
#if !defined __FAKEOUT__
			MPICaptureStatus status;
			uint32_t returnValue = mpiCaptureStatus(capture[axis_number], &status);
			if (returnValue == MPIMessageOK)
			{
				position = status.latchedValue;
			}
#endif
			return position;
		}
	};
}
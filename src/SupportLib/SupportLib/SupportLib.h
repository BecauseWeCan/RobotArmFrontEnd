// SupportLib.h
#pragma once
using namespace System;

//#define __FAKEOUT__
#define INTERNAL_SCALE	(1000)
#define ROBOT_AXES		(6)
#define MAX_AXES		(6)
#define ENGRAVER_AXES	(4)
#define CHECK_RETURN(x) if((x)!=MPIMessageOK)return(x);

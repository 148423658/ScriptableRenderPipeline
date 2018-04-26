static const float2 PoissonDisk[64] =
{
	float2 ( 0.1187053,   0.7951565),
	float2 ( 0.1173675,   0.6087878),
	float2 (-0.09958518,  0.7248842),
	float2 ( 0.4259812,   0.6152718),
	float2 ( 0.3723574,   0.8892787),
	float2 (-0.02289676,  0.9972908),
	float2 (-0.08234791,  0.5048386),
	float2 ( 0.1821235,   0.9673787),
	float2 (-0.2137264,   0.9011746),
	float2 ( 0.3115066,   0.4205415),
	float2 ( 0.1216329,   0.383266),
	float2 ( 0.5948939,   0.7594361),
	float2 ( 0.7576465,   0.5336417),
	float2 (-0.521125,    0.7599803),
	float2 (-0.2923127,   0.6545699),
	float2 ( 0.6782473,   0.22385),
	float2 (-0.3077152,   0.4697627),
	float2 ( 0.4484913,   0.2619455),
	float2 (-0.5308799,   0.4998215),
	float2 (-0.7379634,   0.5304936),
	float2 ( 0.02613133,  0.1764302),
	float2 (-0.1461073,   0.3047384),
	float2 (-0.8451027,   0.3249073),
	float2 (-0.4507707,   0.2101997),
	float2 (-0.6137282,   0.3283674),
	float2 (-0.2385868,   0.08716244),
	float2 ( 0.3386548,   0.01528411),
	float2 (-0.04230833, -0.1494652),
	float2 ( 0.167115,   -0.1098648),
	float2 (-0.525606,    0.01572019),
	float2 (-0.7966855,   0.1318727),
	float2 ( 0.5704287,   0.4778273),
	float2 (-0.9516637,   0.002725032),
	float2 (-0.7068223,  -0.1572321),
	float2 ( 0.2173306,  -0.3494083),
	float2 ( 0.06100426, -0.4492816),
	float2 ( 0.2333982,   0.2247189),
	float2 ( 0.07270987, -0.6396734),
	float2 ( 0.4670808,  -0.2324669),
	float2 ( 0.3729528,  -0.512625),
	float2 ( 0.5675077,  -0.4054544),
	float2 (-0.3691984,  -0.128435),
	float2 ( 0.8752473,   0.2256988),
	float2 (-0.2680127,  -0.4684393),
	float2 (-0.1177551,  -0.7205751),
	float2 (-0.1270121,  -0.3105424),
	float2 ( 0.5595394,  -0.06309237),
	float2 (-0.9299136,  -0.1870008),
	float2 ( 0.974674,    0.03677348),
	float2 ( 0.7726735,  -0.06944724),
	float2 (-0.4995361,  -0.3663749),
	float2 ( 0.6474168,  -0.2315787),
	float2 ( 0.1911449,  -0.8858921),
	float2 ( 0.3671001,  -0.7970535),
	float2 (-0.6970353,  -0.4449432),
	float2 (-0.417599,   -0.7189326),
	float2 (-0.5584748,  -0.6026504),
	float2 (-0.02624448, -0.9141423),
	float2 ( 0.565636,   -0.6585149),
	float2 (-0.874976,   -0.3997879),
	float2 ( 0.9177843,  -0.2110524),
	float2 ( 0.8156927,  -0.3969557),
	float2 (-0.2833054,  -0.8395444),
	float2 ( 0.799141,   -0.5886372)
};

real PenumbraSize(real Reciever, real Blocker)
{
    return abs((Reciever - Blocker) / Blocker);
}

bool BlockerSearch(inout real AverageBlockerDepth, inout real NumBlockers, real LightArea, real2 Noise, real3 Coord, float Slice, Texture2DArray ShadowMap, SamplerState PointSampler)
{
    real BlockerSum = 0.0;
    for (int i = 0; i < 64; ++i)
    {
		real2 Offset = real2(PoissonDisk[i].x * +Noise.y + PoissonDisk[i].y * Noise.x, 
						     PoissonDisk[i].x * -Noise.x + PoissonDisk[i].y * Noise.y) * LightArea;
        real ShadowMapDepth = SAMPLE_TEXTURE2D_ARRAY_LOD( ShadowMap, PointSampler, Coord.xy + Offset, Slice, 0.0 ).x;

        if(ShadowMapDepth > Coord.z)
        {
            BlockerSum  += ShadowMapDepth;
            NumBlockers += 1.0;
        }
    }
    AverageBlockerDepth = BlockerSum / NumBlockers;

    if(NumBlockers < 1) return false;
    else                return true;
}

real PCSS(real3 Coord, real FilterRadius, real2 Noise, real4 ScaleOffset, float Slice, Texture2DArray ShadowMap, SamplerComparisonState CompSampler)
{
	real UMin = ScaleOffset.z;
	real UMax = ScaleOffset.z + ScaleOffset.x;

	real VMin = ScaleOffset.w;
	real VMax = ScaleOffset.w + ScaleOffset.y;

    real Sum = 0.0;
    for(int i = 0; i < 64; ++i)
    {
		real2 Offset = real2(PoissonDisk[i].x * +Noise.y + PoissonDisk[i].y * Noise.x, 
						     PoissonDisk[i].x * -Noise.x + PoissonDisk[i].y * Noise.y) * FilterRadius;

		real U = Coord.x + Offset.x;
		real V = Coord.y + Offset.y;

		//NOTE: We must clamp the sampling within the bounds of the shadow atlas.
		//		Overfiltering will leak results from other shadow lights.
		//TODO: Investigate moving this to blocker search.
		if(U <= UMin || U >= UMax
		|| V <= VMin || V >= VMax)
		{
			Sum += SAMPLE_TEXTURE2D_ARRAY_SHADOW(ShadowMap, CompSampler, real3(Coord.xy, Coord.z), Slice);
		}
		else
		{
    		Sum += SAMPLE_TEXTURE2D_ARRAY_SHADOW(ShadowMap, CompSampler, real3(U, V, Coord.z), Slice);
		}
    }

    return Sum / 64.0;
}
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public static class Terraced
{
    [BurstCompile]
    public struct TerracedJob : IJob
    {
        [ReadOnly][DeallocateOnJobCompletion] 
        public NativeArray<int>    indices;

        [ReadOnly][DeallocateOnJobCompletion] 
        public NativeArray<float3> vertices;
        
        [ReadOnly][DeallocateOnJobCompletion] 
        public NativeArray<float2> uvs;

        [WriteOnly] public NativeList<float3>   v;
        [WriteOnly] public NativeList<float2>   u;
        [WriteOnly] public NativeList<int>      i;

        [ReadOnly] public Entity entity;
        [ReadOnly] public BufferFromEntity<MapPointBuffer> bufferFromEntity;

        public void Execute()
        {
            var buffer = bufferFromEntity[entity];
            
            int l = 0;
            for (int index = 0; index < indices.Length; index += 3)
            {
                float3 a = vertices[indices[index    ]] + new float3(0, buffer[indices[index    ]].Height, 0);
                float3 b = vertices[indices[index + 1]] + new float3(0, buffer[indices[index + 1]].Height, 0);
                float3 c = vertices[indices[index + 2]] + new float3(0, buffer[indices[index + 2]].Height, 0);

                l = TerracedTriangle(a, b, c, ref v, ref u, ref i, l);
            }
        }
    }
    public static int TerracedTriangle(float3 a, float3 b, float3 c, ref NativeList<float3> v, ref NativeList<float2> u, ref NativeList<int> t, int tindex, float step = 0.5f)
    {
        // Minimum height value
        float minHeight = math.floor(math.min(a.y, math.min(b.y, c.y)));

        // Maximum height value
        float maxHeight = math.floor(math.max(a.y, math.max(b.y, c.y)));

        int tCount = tindex;
        float3 v1, v2, v3;

        for (float i = minHeight; i <= maxHeight; i++)
        {
            int state;

            // Find how many points are above or below the slice
            if (a.y < i)
            {
                if (b.y < i)
                {
                    if (c.y < i)
                    {
                        // All points are below
                        v1 = a;
                        v2 = b;
                        v3 = c;

                        state = 0;
                    }
                    else
                    {
                        // Only c is above
                        v1 = b;
                        v2 = c;
                        v3 = a;

                        state = 1;
                    }
                }
                else
                {
                    if (c.y < i)
                    {
                        // Only b is above
                        v1 = a;
                        v2 = b;
                        v3 = c;

                        state = 1;
                    }
                    else
                    {
                        // b and c is above
                        v1 = b;
                        v2 = a;
                        v3 = c;

                        state = 2;
                    }
                }
            }
            else
            {
                if (b.y < i)
                {
                    if (c.y < i)
                    {
                        // Only a is above
                        v1 = c;
                        v2 = a;
                        v3 = b;

                        state = 1;
                    }
                    else
                    {
                        // a and c is above
                        v1 = c;
                        v2 = b;
                        v3 = a;

                        state = 2;
                    }
                }
                else
                {
                    if (c.y < i)
                    {
                        // a and b is above
                        v1 = a;
                        v2 = c;
                        v3 = b;

                        state = 2;
                    }
                    else
                    {
                        // a b c is above
                        v1 = a;
                        v2 = b;
                        v3 = c;

                        state = 3;
                    }
                }
            }

            var height = i * step;

            var v0_u = new float3(v1.x, height, v1.z);
            var v1_u = new float3(v2.x, height, v2.z);
            var v2_u = new float3(v3.x, height, v3.z);

            if (state == 3)
            {
                v.Add(v0_u);
                v.Add(v1_u);
                v.Add(v2_u);

                t.Add(0 + tCount);
                t.Add(1 + tCount);
                t.Add(2 + tCount);

                u.Add(new float2(0, 0));
                u.Add(new float2(0, 1));
                u.Add(new float2(1, 1));

                tCount += 3;
            }
            else
            {
                var height_step = height - step;

                var h1 = v1.y;
                var h2 = v2.y;
                var h3 = v3.y;

                var v0_d = new float3(v1.x, height_step, v1.z);
                var v1_d = new float3(v2.x, height_step, v2.z);
                var v2_d = new float3(v3.x, height_step, v3.z);

                float t0 = (h1 - i) / (h1 - h2);
                float t1 = (h3 - i) / (h3 - h2);

                var v0_d_t = math.lerp(v0_d, v1_d, t0);
                var v0_u_t = math.lerp(v0_u, v1_u, t0);

                var v2_d_t = math.lerp(v2_d, v1_d, t1);
                var v2_u_t = math.lerp(v2_u, v1_u, t1);

                if (state == 1)
                {
                    v.Add(v0_d_t); v.Add(v0_u_t); v.Add(v2_u_t);
                    v.Add(v0_d_t); v.Add(v2_u_t); v.Add(v2_d_t);
                    v.Add(v0_u_t); v.Add(v1_u);   v.Add(v2_u_t);


                    t.Add(0 + tCount); t.Add(1 + tCount); t.Add(2 + tCount);
                    t.Add(3 + tCount); t.Add(4 + tCount); t.Add(5 + tCount);
                    t.Add(6 + tCount); t.Add(7 + tCount); t.Add(8 + tCount);

                    u.Add(new float2(0, 0)); u.Add(new float2(0, 1)); u.Add(new float2(1, 1));
                    u.Add(new float2(0, 0)); u.Add(new float2(0, 1)); u.Add(new float2(1, 1));
                    u.Add(new float2(0, 0)); u.Add(new float2(0, 1)); u.Add(new float2(1, 1));

                    tCount += 9;
                }
                else
                {
                    v.Add(v0_d_t); v.Add(v0_u_t); v.Add(v2_u_t);
                    v.Add(v0_d_t); v.Add(v2_u_t); v.Add(v2_d_t);
                    v.Add(v0_u_t); v.Add(v0_u);   v.Add(v2_u);
                    v.Add(v0_u_t); v.Add(v2_u);   v.Add(v2_u_t);

                    t.Add(0 + tCount); t.Add( 1 + tCount); t.Add(2 + tCount);
                    t.Add(3 + tCount); t.Add( 4 + tCount); t.Add(5 + tCount);
                    t.Add(6 + tCount); t.Add( 7 + tCount); t.Add(8 + tCount);
                    t.Add(9 + tCount); t.Add(10 + tCount); t.Add(11 + tCount);

                    u.Add(new float2(0, 0)); u.Add(new float2(0, 1)); u.Add(new float2(1, 1));
                    u.Add(new float2(0, 0)); u.Add(new float2(0, 1)); u.Add(new float2(1, 1));
                    u.Add(new float2(0, 0)); u.Add(new float2(0, 1)); u.Add(new float2(1, 1));
                    u.Add(new float2(0, 0)); u.Add(new float2(0, 1)); u.Add(new float2(1, 1));

                    tCount += 12;
                }

            }

        }

        return tCount;

    }
}

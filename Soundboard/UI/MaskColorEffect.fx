sampler2D implicitInput : register(s0);

float4 colorBlack : register(c0);
float4 colorWhite : register(c1);

float adjustBlack : register(c2);
float adjustWhite : register(c3);

sampler2D maskInput : register(s1);


float2 spherize(float2 UV, float2 Center, float Strength, float2 Offset)
{
    float2 delta = UV - Center;
    float delta2 = dot(delta.xy, delta.xy);
    float delta4 = delta2 * delta2;
    float2 delta_offset = delta4 * Strength;
    return UV + delta * delta_offset + Offset;
}
// b.x = width
// b.y = height
// r.x = roundness top-right  
// r.y = roundness boottom-right
// r.z = roundness top-left
// r.w = roundness bottom-left
float sdRoundBox(in float2 p, in float2 b, in float4 r)
{
    r.xy = (p.x > 0.0) ? r.xy : r.zw;
    r.x = (p.y > 0.0) ? r.x : r.y;
    float2 q = abs(p) - b + r.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
}

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 color = tex2D(implicitInput, uv);

    float2 centered = (uv - 0.5) * 3;

    float2 tiled = spherize(uv, float2(0.5, 0.5), -14, float2(0, 0));

    float radius = 0.1;

    float mask = 1 - sdRoundBox(centered, float2(0.4, 0.4), float4(radius, radius, radius, radius));
    mask *= 0.7;

    mask = 1 - ((1 - mask) * 0.66*length(centered));
    mask = saturate(mask) * 0.9;

    float4 Color = colorBlack;

    float4 clr1 = colorWhite * float4(mask, mask, mask, 1);

    float4 res = float4(0, 0, 0, 1);



    if (Color.r < 0.5)
        res.r = 2 * clr1.r * Color.r;
    else
        res.r = 1 - (2 * (1 - Color.r) * (1 - clr1.r));

    if (Color.g < 0.5)
        res.g = 2 * clr1.g * Color.g;
    else
        res.g = 1 - (2 * (1 - Color.g) * (1 - clr1.g));

    if (Color.b < 0.5)
        res.b = 2 * clr1.b * Color.b;
    else
        res.b = 1 - (2 * (1 - Color.b) * (1 - clr1.b));

    return color * res;










    //return float4(r, r, r, 1.0);// color + (colorBlack - color) * adjustBlack;
}

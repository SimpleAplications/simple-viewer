// Picture adjustments, applied on the graphics card to whatever is on screen.
// Compile with: fxc /T ps_2_0 /E main /Fo Adjust.ps Adjust.fx

sampler2D input : register(s0);
float brightness : register(c0);   // -0.5 .. 0.5, 0 = unchanged
float contrast : register(c1);     // 0 .. 2, 1 = unchanged
float saturation : register(c2);   // 0 .. 2, 1 = unchanged
float gamma : register(c3);        // 0.4 .. 2.5, 1 = unchanged
float warmth : register(c4);       // -1 .. 1, 0 = unchanged

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 pixel = tex2D(input, uv);
    // WPF hands over colours multiplied by alpha; work on the plain colour.
    float alpha = max(pixel.a, 0.0001);
    float3 colour = pixel.rgb / alpha;

    colour = (colour - 0.5) * contrast + 0.5 + brightness;
    float grey = dot(colour, float3(0.2126, 0.7152, 0.0722));
    colour = lerp(float3(grey, grey, grey), colour, saturation);
    colour.r += warmth * 0.12;
    colour.b -= warmth * 0.12;
    colour = pow(saturate(colour), 1.0 / gamma);

    return float4(colour * pixel.a, pixel.a);
}

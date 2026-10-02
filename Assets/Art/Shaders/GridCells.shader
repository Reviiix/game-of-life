// Draws the Game of Life grid from a texture holding one pixel per cell.
// TEXCOORD1 carries (columns, rows) and TEXCOORD2 carries line thickness as a fraction of a cell,
// both written by GridView so no per-instance material is needed.
Shader "GameOfLife/UI/GridCells"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cell Colours", 2D) = "white" {}

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct VertexInput
            {
                float4 vertex : POSITION;
                fixed4 lineColour : COLOR;
                float2 cellUv : TEXCOORD0;
                float2 gridSize : TEXCOORD1;
                float2 lineThickness : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                fixed4 lineColour : COLOR;
                float2 cellUv : TEXCOORD0;
                float2 gridSize : TEXCOORD1;
                float2 lineThickness : TEXCOORD2;
                float4 worldPosition : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _ClipRect;

            FragmentInput vert(VertexInput input)
            {
                FragmentInput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.position = UnityObjectToClipPos(input.vertex);
                output.lineColour = input.lineColour;
                output.cellUv = input.cellUv;
                output.gridSize = input.gridSize;
                output.lineThickness = input.lineThickness;
                return output;
            }

            fixed4 frag(FragmentInput input) : SV_Target
            {
                float2 positionInCells = input.cellUv * input.gridSize;
                float2 positionInsideCell = frac(positionInCells);
                float2 cellCoordinate = floor(positionInCells);

                // Lines sit along the right and bottom edge of every cell except the last column and row.
                float2 isNotLastCell = step(cellCoordinate, input.gridSize - 1.5);
                float2 isOnLine = step(1.0 - input.lineThickness, positionInsideCell) * step(0.0001, input.lineThickness) * isNotLastCell;

                fixed4 colour = tex2D(_MainTex, input.cellUv);
                colour = lerp(colour, input.lineColour, saturate(isOnLine.x + isOnLine.y));

                #ifdef UNITY_UI_CLIP_RECT
                colour.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                return colour;
            }
            ENDCG
        }
    }
}

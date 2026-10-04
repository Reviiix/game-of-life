// Draws the Game of Life board as rounded tiles from a texture holding one pixel per cell.
// Each pixel's RGB is the tile colour and its alpha is the animated tile size (alpha 0 is an empty cell; see CellAnimator).
// TEXCOORD1 carries (columns, rows, cell width, cell height) and TEXCOORD2 carries (tile gap, corner radius), all in canvas units,
// and the vertex colour is the faint empty-tile colour, so no per-instance material is needed.
Shader "GameOfLife/UI/GridCells"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cell Colours And Sizes", 2D) = "black" {}

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
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            // Must match CellAnimator.MaximumEncodedScale.
            #define MAXIMUM_ENCODED_SCALE 1.5

            struct VertexInput
            {
                float4 vertex : POSITION;
                fixed4 emptyTileColour : COLOR;
                float2 cellUv : TEXCOORD0;
                float4 gridShape : TEXCOORD1;
                float4 tileShape : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                fixed4 emptyTileColour : COLOR;
                float2 cellUv : TEXCOORD0;
                float4 gridShape : TEXCOORD1;
                float4 tileShape : TEXCOORD2;
                float4 worldPosition : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _ClipRect;
            float _UIVertexColorAlwaysGammaSpace;

            FragmentInput vert(VertexInput input)
            {
                FragmentInput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.position = UnityObjectToClipPos(input.vertex);
                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                {
                    input.emptyTileColour.rgb = UIGammaToLinear(input.emptyTileColour.rgb);
                }
                output.emptyTileColour = input.emptyTileColour;
                output.cellUv = input.cellUv;
                output.gridShape = input.gridShape;
                output.tileShape = input.tileShape;
                return output;
            }

            // Signed distance from a point to a rounded rectangle centred on the origin; negative inside.
            float RoundedRectangleDistance(float2 position, float2 halfSize, float cornerRadius)
            {
                float2 cornerOffset = abs(position) - halfSize + cornerRadius;
                return length(max(cornerOffset, 0.0)) + min(max(cornerOffset.x, cornerOffset.y), 0.0) - cornerRadius;
            }

            // How much of this pixel a tile covers, anti-aliased over one screen pixel.
            float TileCoverage(float2 positionInCell, float2 tileHalfSize, float cornerRadius, float pixelSize)
            {
                float radius = min(cornerRadius, min(tileHalfSize.x, tileHalfSize.y));
                float distance = RoundedRectangleDistance(positionInCell, tileHalfSize, radius);
                return saturate(0.5 - distance / pixelSize);
            }

            fixed4 frag(FragmentInput input) : SV_Target
            {
                float2 gridSize = input.gridShape.xy;
                float2 cellSize = input.gridShape.zw;
                float tileGap = input.tileShape.x;
                float cornerRadius = input.tileShape.y;

                // Canvas units per screen pixel, from the continuous board position so cell edges never spike the anti-aliasing.
                float2 boardPosition = input.cellUv * gridSize * cellSize;
                float pixelSize = max(length(float2(ddx(boardPosition.x), ddy(boardPosition.x))), length(float2(ddx(boardPosition.y), ddy(boardPosition.y))));
                pixelSize = max(pixelSize, 1e-4);

                float2 positionInCells = input.cellUv * gridSize;
                float2 cellCoordinate = min(floor(positionInCells), gridSize - 1.0);
                float2 positionInCell = (positionInCells - cellCoordinate - 0.5) * cellSize;

                // Sample at the cell's centre so every pixel of a tile reads the same texel.
                fixed4 cell = tex2D(_MainTex, (cellCoordinate + 0.5) / gridSize);
                float2 restingHalfSize = max(cellSize * 0.5 - tileGap * 0.5, 0.0);

                // Never grow past the cell on either axis, where the tile would be clipped by its neighbour's pixels.
                float2 growLimit = cellSize * 0.5 / max(restingHalfSize, 1e-4);
                float tileScale = min(cell.a * MAXIMUM_ENCODED_SCALE, min(growLimit.x, growLimit.y));

                float emptyCoverage = TileCoverage(positionInCell, restingHalfSize, cornerRadius, pixelSize) * input.emptyTileColour.a;
                float livingCoverage = cell.a > 0.0 ? TileCoverage(positionInCell, restingHalfSize * tileScale, cornerRadius * tileScale, pixelSize) : 0.0;

                fixed4 colour;
                colour.a = livingCoverage + emptyCoverage * (1.0 - livingCoverage);
                colour.rgb = (cell.rgb * livingCoverage + input.emptyTileColour.rgb * emptyCoverage * (1.0 - livingCoverage)) / max(colour.a, 1e-4);

                #ifdef UNITY_UI_CLIP_RECT
                colour.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                return colour;
            }
            ENDCG
        }
    }
}

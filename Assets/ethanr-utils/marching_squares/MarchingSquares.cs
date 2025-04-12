using System;
using UnityEngine;
using UnityUtilities.General;
using UnityUtilities.Meshing;

namespace ethanr_utils.marching_squares
{
    /// <summary>
    /// A class used to make meshes from 2D voxel data.
    /// </summary>
    public class MarchingSquares
    {
        /// <summary>
        /// Generate a mesh from a provided voxel map.
        /// </summary>
        /// <param name="voxelMap">Voxels provided with x,y=0,0 at the bottom left.</param>
        /// <param name="isoLine">The isoline at which to draw the boundary.</param>
        /// <param name="area">The area the mesh should generate into.</param>
        /// <returns></returns>
        public static Mesh GenerateMesh(float[,] voxelMap, float isoLine, Rect area)
        {
            /* Ensure we have enough voxels */
            if (voxelMap.GetLength(0) < 2 || voxelMap.GetLength(1) < 2)
            {
                throw new ArgumentException("Voxel map must be at least 2x2.");
            }
            
            /* Figure out cell size */
            Vector2 cellSize = new Vector2(area.width / voxelMap.GetLength(0), area.height / voxelMap.GetLength(1));
            Vector2 cornerOffset = cellSize / 2.0f;
            
            /* Generate Cells from provided voxels */
            MarchingSquaresCell[,] cells = new MarchingSquaresCell[voxelMap.GetLength(0)-1, voxelMap.GetLength(1)-1];
            for (int x = 0; x < voxelMap.GetLength(0)-1; x++)
            {
                for (int y = 0; y < voxelMap.GetLength(1) - 1; y++)
                {
                    var curr = new MarchingSquaresCell(
                        cornerOffset.ScaleBy(x + 1, y + 1), voxelMap[x, y],
                        cornerOffset.ScaleBy(x + 2, y + 1), voxelMap[x + 1, y],
                        cornerOffset.ScaleBy(x + 2, y + 2), voxelMap[x + 1, y + 1],
                        cornerOffset.ScaleBy(x + 1, y + 2), voxelMap[x, y + 1],
                        isoLine);
                    cells[x, y] = curr;
                }
            }
            
            /* We can now generate a mesh bit by bit from the voxel cells */
            Mesher mesher = new Mesher(false);
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    /* Figure out correct format */
                    var curr = cells[x, y];
                    switch (curr.ID)
                    {
                        case 0:         //FFFF
                            /* Skip - empty cell */
                            break;
                        case 1:         //TFFF
                            /* Simple triangle */
                            var vertLerpAmt = ReverseLerp(curr.Values[0], curr.Values[3], isoLine);
                            var horizLerpAmt = ReverseLerp(curr.Values[0], curr.Values[1], isoLine);
                            mesher.AddTriangle(curr.Corners[0], 
                                Vector2.Lerp(curr.Corners[0], curr.Corners[3], vertLerpAmt),
                                Vector2.Lerp(curr.Corners[0], curr.Corners[1], horizLerpAmt) 
                                );
                            break;
                        case 2:         //FTFF
                            /* Simple triangle */
                            vertLerpAmt = ReverseLerp(curr.Values[1], curr.Values[2], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[1], curr.Values[0], isoLine);
                            mesher.AddTriangle(curr.Corners[1], 
                                Vector2.Lerp(curr.Corners[1], curr.Corners[0], horizLerpAmt),
                                Vector2.Lerp(curr.Corners[1], curr.Corners[2], vertLerpAmt)
                            );
                            break;
                        case 3:         //TTFF
                            /* Flattened quad */
                            var leftLerpAmt= ReverseLerp(curr.Values[0], curr.Values[3], isoLine);
                            var rightLerpAmt = ReverseLerp(curr.Values[1], curr.Values[2], isoLine);
                            mesher.AddQuad(
                                curr.Corners[0],
                                Vector2.Lerp(curr.Corners[0], curr.Corners[3], leftLerpAmt),
                                Vector2.Lerp(curr.Corners[1], curr.Corners[2], rightLerpAmt),
                                curr.Corners[1],
                                Color.white);
                            break;
                        case 4:         //FFTF
                            /* Simple triangle */
                            vertLerpAmt = ReverseLerp(curr.Values[2], curr.Values[1], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[2], curr.Values[3], isoLine);
                            mesher.AddTriangle(curr.Corners[2], 
                                Vector2.Lerp(curr.Corners[2], curr.Corners[1], vertLerpAmt),
                                Vector2.Lerp(curr.Corners[2], curr.Corners[3], horizLerpAmt)
                            );
                            break;
                        case 5:         //TFTF
                            /* Saddle point */
                            /* Sample the center of the cell */
                            var center = curr.GetCenter();
                            var centerVal = curr.GetCenterValue();
                            if (centerVal >= isoLine)
                            {
                                /* T-T are together */
                                /* Perform interpolation */
                                var brLerp = ReverseLerp(curr.Values[0], curr.Values[1], isoLine);
                                var btLerp = ReverseLerp(curr.Values[0], curr.Values[3], isoLine);
                                var ctlLerp = ReverseLerp(centerVal, curr.Values[3], isoLine);
                                var tlLerp = ReverseLerp(curr.Values[2], curr.Values[3], isoLine);
                                var tbLerp = ReverseLerp(curr.Values[2], curr.Values[1], isoLine);
                                var cbrLerp = ReverseLerp(centerVal, curr.Values[2], isoLine);
                                mesher.AddFan(new Vector3[]
                                {
                                    curr.Corners[0],
                                    Vector2.Lerp(curr.Corners[0], curr.Corners[3], btLerp),
                                    Vector2.Lerp(center, curr.Corners[3], ctlLerp),
                                    Vector2.Lerp(center, curr.Corners[1], cbrLerp),
                                    Vector2.Lerp(curr.Corners[0], curr.Corners[1], brLerp),
                                });
                                mesher.AddFan(new Vector3[]
                                {
                                    curr.Corners[2],
                                    Vector2.Lerp(curr.Corners[2], curr.Corners[1], tbLerp),
                                    Vector2.Lerp(center, curr.Corners[1], cbrLerp),
                                    Vector2.Lerp(center, curr.Corners[3], ctlLerp),
                                    Vector2.Lerp(curr.Corners[2], curr.Corners[3], tlLerp),
                                });
                            }
                            else
                            {
                                /* T-T are separated */
                                /* Perform interpolation */
                                var brLerp = ReverseLerp(curr.Values[0], curr.Values[1], isoLine);
                                var btLerp = ReverseLerp(curr.Values[0], curr.Values[3], isoLine);
                                var bcLerp = ReverseLerp(curr.Values[0], centerVal, isoLine);
                                mesher.AddQuad(curr.Corners[0],
                                    Vector2.Lerp(curr.Corners[0], curr.Corners[3], btLerp),
                                    Vector2.Lerp(curr.Corners[0], center, bcLerp),
                                    Vector2.Lerp(curr.Corners[0], curr.Corners[1], brLerp),
                                    Color.white);
                                
                                var tlLerp = ReverseLerp(curr.Values[2], curr.Values[3], isoLine);
                                var tbLerp = ReverseLerp(curr.Values[2], curr.Values[1], isoLine);
                                var tcLerp = ReverseLerp(curr.Values[2], centerVal, isoLine);
                                mesher.AddQuad(curr.Corners[2],
                                    Vector2.Lerp(curr.Corners[2], curr.Corners[1], tbLerp),
                                    Vector2.Lerp(curr.Corners[2], center, tcLerp),
                                    Vector2.Lerp(curr.Corners[2], curr.Corners[3], tlLerp),
                                    Color.white);
                            }
                            break;
                        case 6:         //FTTF
                            /* Flattened Quad */
                            var topLerpAmt = ReverseLerp(curr.Values[2], curr.Values[3], isoLine);
                            var botLerpAmt = ReverseLerp(curr.Values[1], curr.Values[0], isoLine);
                            mesher.AddQuad(curr.Corners[2],
                                curr.Corners[1],
                                Vector2.Lerp(curr.Corners[1], curr.Corners[0], botLerpAmt),
                                Vector2.Lerp(curr.Corners[2], curr.Corners[3], topLerpAmt),
                                Color.white);
                            break;
                        case 7:         //TTTF
                            /* A polygon */
                            vertLerpAmt = ReverseLerp(curr.Values[0], curr.Values[3], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[2], curr.Values[3], isoLine);
                            Vector3 vertPoint = Vector2.Lerp(curr.Corners[0], curr.Corners[3], vertLerpAmt);
                            Vector3 horizPoint = Vector2.Lerp(curr.Corners[2], curr.Corners[3], horizLerpAmt);
                            mesher.AddTriangle(curr.Corners[0],
                                vertPoint,
                                curr.Corners[1]);
                            mesher.AddTriangle(vertPoint,
                                horizPoint,
                                curr.Corners[1]);
                            mesher.AddTriangle(horizPoint,
                                curr.Corners[2],
                                curr.Corners[1]);
                            break;
                        case 8:         //FFFT
                            /* Simple triangle */
                            vertLerpAmt = ReverseLerp(curr.Values[3], curr.Values[0], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[3], curr.Values[2], isoLine);
                            mesher.AddTriangle(curr.Corners[3], 
                                Vector2.Lerp(curr.Corners[3], curr.Corners[2], horizLerpAmt),
                                Vector2.Lerp(curr.Corners[3], curr.Corners[0], vertLerpAmt) 
                            );
                            break;
                        case 9:         //TFFT
                            /* Flattened Quad */
                            topLerpAmt = ReverseLerp(curr.Values[3], curr.Values[2], isoLine);
                            botLerpAmt = ReverseLerp(curr.Values[0], curr.Values[1], isoLine);
                            mesher.AddQuad(curr.Corners[0],
                                curr.Corners[3], 
                                Vector2.Lerp(curr.Corners[3], curr.Corners[2], topLerpAmt),
                                Vector2.Lerp(curr.Corners[0], curr.Corners[1], botLerpAmt),
                                Color.white);
                            break;
                        case 10:        //FTFT
                            /* Saddle point */
                            /* Sample the center of the cell */
                            center = curr.GetCenter();
                            centerVal = curr.GetCenterValue();
                            if (centerVal >= isoLine)
                            {
                                /* T-T are together */
                                /* Perform interpolation */
                                var tbLerp = ReverseLerp(curr.Values[3], curr.Values[0], isoLine);
                                var trLerp = ReverseLerp(curr.Values[3], curr.Values[2], isoLine);
                                var blLerp = ReverseLerp(curr.Values[1], curr.Values[0], isoLine);
                                var btLerp = ReverseLerp(curr.Values[1], curr.Values[2], isoLine);
                                var ctrLerp = ReverseLerp(centerVal, curr.Values[2], isoLine);
                                var cblLerp = ReverseLerp(centerVal, curr.Values[0], isoLine);
                                mesher.AddFan(new Vector3[]
                                {
                                    curr.Corners[3],
                                    Vector2.Lerp(curr.Corners[3], curr.Corners[2], trLerp),
                                    Vector2.Lerp(center, curr.Corners[2], ctrLerp),
                                    Vector2.Lerp(center, curr.Corners[0], cblLerp),
                                    Vector2.Lerp(curr.Corners[3], curr.Corners[0], tbLerp),
                                });
                                mesher.AddFan(new Vector3[]
                                {
                                    curr.Corners[1],
                                    Vector2.Lerp(curr.Corners[1], curr.Corners[0], blLerp),
                                    Vector2.Lerp(center, curr.Corners[0], cblLerp),
                                    Vector2.Lerp(center, curr.Corners[2], ctrLerp),
                                    Vector2.Lerp(curr.Corners[1], curr.Corners[2], btLerp)
                                });
                            }
                            else
                            {
                                /* T-T are separated */
                                /* Perform interpolation */
                                var tbLerp = ReverseLerp(curr.Values[3], curr.Values[0], isoLine);
                                var trLerp = ReverseLerp(curr.Values[3], curr.Values[2], isoLine);
                                var tcLerp = ReverseLerp(curr.Values[3], centerVal, isoLine);
                                mesher.AddQuad(curr.Corners[3],
                                    Vector2.Lerp(curr.Corners[3], curr.Corners[2], trLerp),
                                    Vector2.Lerp(curr.Corners[3], center, tcLerp),
                                    Vector2.Lerp(curr.Corners[3], curr.Corners[0], tbLerp),
                                    Color.white);
                                
                                var blLerp = ReverseLerp(curr.Values[1], curr.Values[0], isoLine);
                                var btLerp = ReverseLerp(curr.Values[1], curr.Values[2], isoLine);
                                var bcLerp = ReverseLerp(curr.Values[1], centerVal, isoLine);
                                mesher.AddQuad(curr.Corners[1],
                                    Vector2.Lerp(curr.Corners[1], curr.Corners[0], blLerp),
                                    Vector2.Lerp(curr.Corners[1], center, bcLerp),
                                    Vector2.Lerp(curr.Corners[1], curr.Corners[2], btLerp),
                                    Color.white);
                            }
                            break;
                        case 11:        //TTFT
                            /* A polygon */
                            vertLerpAmt = ReverseLerp(curr.Values[1], curr.Values[2], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[3], curr.Values[2], isoLine);
                            vertPoint = Vector2.Lerp(curr.Corners[1], curr.Corners[2], vertLerpAmt);
                            horizPoint = Vector2.Lerp(curr.Corners[3], curr.Corners[2], horizLerpAmt);
                            mesher.AddTriangle(
                                curr.Corners[0],
                                curr.Corners[3],
                                horizPoint
                                );
                            mesher.AddTriangle(
                                horizPoint,
                                vertPoint,
                                curr.Corners[0]
                            );
                            mesher.AddTriangle(
                                vertPoint,
                                curr.Corners[1],
                                curr.Corners[0]
                            );
                            break;
                        case 12:        //FFTT
                            /* Flattened quad */
                            leftLerpAmt= ReverseLerp(curr.Values[3], curr.Values[0], isoLine);
                            rightLerpAmt = ReverseLerp(curr.Values[2], curr.Values[1], isoLine);
                            mesher.AddQuad(
                                curr.Corners[3],
                                curr.Corners[2],
                                Vector2.Lerp(curr.Corners[2], curr.Corners[1], rightLerpAmt),
                                Vector2.Lerp(curr.Corners[3], curr.Corners[0], leftLerpAmt),
                                Color.white);
                            break;
                        case 13:        //TFTT
                            /* A polygon */
                            vertLerpAmt = ReverseLerp(curr.Values[2], curr.Values[1], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[0], curr.Values[1], isoLine);
                            vertPoint = Vector2.Lerp(curr.Corners[2], curr.Corners[1], vertLerpAmt);
                            horizPoint = Vector2.Lerp(curr.Corners[0], curr.Corners[1], horizLerpAmt);
                            mesher.AddTriangle(
                                curr.Corners[0],
                                curr.Corners[3],
                                horizPoint
                            );
                            mesher.AddTriangle(
                                horizPoint,
                                curr.Corners[3],
                                vertPoint
                            );
                            mesher.AddTriangle(
                                vertPoint,
                                curr.Corners[3],
                                curr.Corners[2]
                            );
                            break;
                        case 14:        //FTTT
                            /* A polygon */
                            vertLerpAmt = ReverseLerp(curr.Values[3], curr.Values[0], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[1], curr.Values[0], isoLine);
                            vertPoint = Vector2.Lerp(curr.Corners[3], curr.Corners[0], vertLerpAmt);
                            horizPoint = Vector2.Lerp(curr.Corners[1], curr.Corners[0], horizLerpAmt);
                            mesher.AddTriangle(
                                vertPoint,
                                curr.Corners[3],
                                curr.Corners[2]
                            );
                            mesher.AddTriangle(
                                vertPoint,
                                curr.Corners[2],
                                horizPoint
                            );
                            mesher.AddTriangle(
                                horizPoint,
                                curr.Corners[2],
                                curr.Corners[1]
                            );
                            break;
                        case 15:        //TTTT
                            /* One big quad */
                            mesher.AddQuad(
                                curr.Corners[0], curr.Corners[3], curr.Corners[2], curr.Corners[1], Color.white);
                            break;
                        default:
                            break;
                    }
                }
            }
            
            return mesher.GenerateMesh();
        }
        
        /// <summary>
        /// Generate a mesh from a provided voxel map.
        /// </summary>
        /// <param name="voxelMap">Voxels provided with x,y=0,0 at the bottom left.</param>
        /// <param name="isoLine">The isoline at which to draw the boundary.</param>
        /// <param name="area">The area the mesh should generate into.</param>
        /// <returns></returns>
        public static ContourBuilder GenerateMeshContours(float[,] voxelMap, float isoLine, Rect area)
        {
            /* Ensure we have enough voxels */
            if (voxelMap.GetLength(0) < 2 || voxelMap.GetLength(1) < 2)
            {
                throw new ArgumentException("Voxel map must be at least 2x2.");
            }
            
            /* Figure out cell size */
            Vector2 cellSize = new Vector2(area.width / voxelMap.GetLength(0), area.height / voxelMap.GetLength(1));
            Vector2 cornerOffset = cellSize / 2.0f;
            
            /* Generate Cells from provided voxels */
            MarchingSquaresCell[,] cells = new MarchingSquaresCell[voxelMap.GetLength(0)-1, voxelMap.GetLength(1)-1];
            for (int x = 0; x < voxelMap.GetLength(0)-1; x++)
            {
                for (int y = 0; y < voxelMap.GetLength(1) - 1; y++)
                {
                    var curr = new MarchingSquaresCell(
                        cornerOffset.ScaleBy(x + 1, y + 1), voxelMap[x, y],
                        cornerOffset.ScaleBy(x + 2, y + 1), voxelMap[x + 1, y],
                        cornerOffset.ScaleBy(x + 2, y + 2), voxelMap[x + 1, y + 1],
                        cornerOffset.ScaleBy(x + 1, y + 2), voxelMap[x, y + 1],
                        isoLine);
                    cells[x, y] = curr;
                }
            }
            
            /* We can now generate a contour bit by bit from the voxel cells */
            ContourBuilder builder = new ContourBuilder();
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    /* Figure out correct format */
                    var curr = cells[x, y];
                    switch (curr.ID)
                    {
                        case 0:         //FFFF
                            /* Skip - empty cell */
                            break;
                        case 1:         //TFFF
                            /* Simple triangle */
                            var vertLerpAmt = ReverseLerp(curr.Values[0], curr.Values[3], isoLine);
                            var horizLerpAmt = ReverseLerp(curr.Values[0], curr.Values[1], isoLine);
                            builder.AddEdge(Vector2.Lerp(curr.Corners[0], curr.Corners[3], vertLerpAmt),
                                Vector2.Lerp(curr.Corners[0], curr.Corners[1], horizLerpAmt)
                                );
                            break;
                        case 2:         //FTFF
                            /* Simple triangle */
                            vertLerpAmt = ReverseLerp(curr.Values[1], curr.Values[2], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[1], curr.Values[0], isoLine);
                            builder.AddEdge(Vector2.Lerp(curr.Corners[1], curr.Corners[0], horizLerpAmt),
                                Vector2.Lerp(curr.Corners[1], curr.Corners[2], vertLerpAmt));
                            break;
                        case 3:         //TTFF
                            /* Flattened quad */
                            var leftLerpAmt= ReverseLerp(curr.Values[0], curr.Values[3], isoLine);
                            var rightLerpAmt = ReverseLerp(curr.Values[1], curr.Values[2], isoLine);
                            builder.AddEdge(Vector2.Lerp(curr.Corners[0], curr.Corners[3], leftLerpAmt),
                                Vector2.Lerp(curr.Corners[1], curr.Corners[2], rightLerpAmt));
                            break;
                        case 4:         //FFTF
                            /* Simple triangle */
                            vertLerpAmt = ReverseLerp(curr.Values[2], curr.Values[1], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[2], curr.Values[3], isoLine);
                            builder.AddEdge(Vector2.Lerp(curr.Corners[2], curr.Corners[1], vertLerpAmt),
                                Vector2.Lerp(curr.Corners[2], curr.Corners[3], horizLerpAmt));
                            break;
                        case 5:         //TFTF
                            /* Saddle point */
                            /* Sample the center of the cell */
                            var center = curr.GetCenter();
                            var centerVal = curr.GetCenterValue();
                            if (centerVal >= isoLine)
                            {
                                /* T-T are together */
                                /* Perform interpolation */
                                var brLerp = ReverseLerp(curr.Values[0], curr.Values[1], isoLine);
                                var btLerp = ReverseLerp(curr.Values[0], curr.Values[3], isoLine);
                                var ctlLerp = ReverseLerp(centerVal, curr.Values[3], isoLine);
                                var tlLerp = ReverseLerp(curr.Values[2], curr.Values[3], isoLine);
                                var tbLerp = ReverseLerp(curr.Values[2], curr.Values[1], isoLine);
                                var cbrLerp = ReverseLerp(centerVal, curr.Values[2], isoLine);
                                builder.AddEdge(Vector2.Lerp(curr.Corners[0], curr.Corners[3], btLerp),
                                    Vector2.Lerp(center, curr.Corners[3], ctlLerp));
                                builder.AddEdge(Vector2.Lerp(center, curr.Corners[3], ctlLerp),
                                    Vector2.Lerp(curr.Corners[2], curr.Corners[3], tlLerp));
                                builder.AddEdge(Vector2.Lerp(curr.Corners[2], curr.Corners[1], tbLerp),
                                    Vector2.Lerp(center, curr.Corners[1], cbrLerp));
                                builder.AddEdge(Vector2.Lerp(center, curr.Corners[1], cbrLerp),
                                    Vector2.Lerp(curr.Corners[0], curr.Corners[1], brLerp));
                            }
                            else
                            {
                                /* T-T are separated */
                                /* Perform interpolation */
                                var brLerp = ReverseLerp(curr.Values[0], curr.Values[1], isoLine);
                                var btLerp = ReverseLerp(curr.Values[0], curr.Values[3], isoLine);
                                var bcLerp = ReverseLerp(curr.Values[0], centerVal, isoLine);
                                var tlLerp = ReverseLerp(curr.Values[2], curr.Values[3], isoLine);
                                var tbLerp = ReverseLerp(curr.Values[2], curr.Values[1], isoLine);
                                var tcLerp = ReverseLerp(curr.Values[2], centerVal, isoLine);
                                builder.AddEdge(Vector2.Lerp(curr.Corners[0], curr.Corners[3], btLerp),
                                    Vector2.Lerp(curr.Corners[0], center, bcLerp));
                                builder.AddEdge(Vector2.Lerp(curr.Corners[0], center, bcLerp),
                                    Vector2.Lerp(curr.Corners[0], curr.Corners[1], brLerp));
                                builder.AddEdge(Vector2.Lerp(curr.Corners[2], curr.Corners[3], tlLerp),
                                    Vector2.Lerp(curr.Corners[2], center, tcLerp));
                                builder.AddEdge(Vector2.Lerp(curr.Corners[2], center, tcLerp),
                                    Vector2.Lerp(curr.Corners[2], curr.Corners[1], tbLerp));
                            }
                            break;
                        case 6:         //FTTF
                            /* Flattened Quad */
                            var topLerpAmt = ReverseLerp(curr.Values[2], curr.Values[3], isoLine);
                            var botLerpAmt = ReverseLerp(curr.Values[1], curr.Values[0], isoLine);
                            builder.AddEdge(Vector2.Lerp(curr.Corners[1], curr.Corners[0], botLerpAmt),
                                Vector2.Lerp(curr.Corners[2], curr.Corners[3], topLerpAmt));
                            break;
                        case 7:         //TTTF
                            /* A polygon */
                            vertLerpAmt = ReverseLerp(curr.Values[0], curr.Values[3], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[2], curr.Values[3], isoLine);
                            Vector3 vertPoint = Vector2.Lerp(curr.Corners[0], curr.Corners[3], vertLerpAmt);
                            Vector3 horizPoint = Vector2.Lerp(curr.Corners[2], curr.Corners[3], horizLerpAmt);
                            builder.AddEdge(vertPoint, horizPoint);
                            break;
                        case 8:         //FFFT
                            /* Simple triangle */
                            vertLerpAmt = ReverseLerp(curr.Values[3], curr.Values[0], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[3], curr.Values[2], isoLine);
                            builder.AddEdge(Vector2.Lerp(curr.Corners[3], curr.Corners[2], horizLerpAmt),
                                Vector2.Lerp(curr.Corners[3], curr.Corners[0], vertLerpAmt));
                            break;
                        case 9:         //TFFT
                            /* Flattened Quad */
                            topLerpAmt = ReverseLerp(curr.Values[3], curr.Values[2], isoLine);
                            botLerpAmt = ReverseLerp(curr.Values[0], curr.Values[1], isoLine);
                            builder.AddEdge(Vector2.Lerp(curr.Corners[3], curr.Corners[2], topLerpAmt),
                                Vector2.Lerp(curr.Corners[0], curr.Corners[1], botLerpAmt));
                            break;
                        case 10:        //FTFT
                            /* Saddle point */
                            /* Sample the center of the cell */
                            center = curr.GetCenter();
                            centerVal = curr.GetCenterValue();
                            if (centerVal >= isoLine)
                            {
                                /* T-T are together */
                                /* Perform interpolation */
                                var tbLerp = ReverseLerp(curr.Values[3], curr.Values[0], isoLine);
                                var trLerp = ReverseLerp(curr.Values[3], curr.Values[2], isoLine);
                                var blLerp = ReverseLerp(curr.Values[1], curr.Values[0], isoLine);
                                var btLerp = ReverseLerp(curr.Values[1], curr.Values[2], isoLine);
                                var ctrLerp = ReverseLerp(centerVal, curr.Values[2], isoLine);
                                var cblLerp = ReverseLerp(centerVal, curr.Values[0], isoLine);
                                builder.AddEdge(Vector2.Lerp(curr.Corners[1], curr.Corners[0], blLerp),
                                    Vector2.Lerp(center, curr.Corners[0], cblLerp));
                                builder.AddEdge(Vector2.Lerp(center, curr.Corners[0], cblLerp),
                                    Vector2.Lerp(curr.Corners[3], curr.Corners[0], tbLerp));
                                builder.AddEdge(Vector2.Lerp(curr.Corners[3], curr.Corners[2], trLerp),
                                    Vector2.Lerp(center, curr.Corners[2], ctrLerp));
                                builder.AddEdge(Vector2.Lerp(center, curr.Corners[2], ctrLerp),
                                    Vector2.Lerp(curr.Corners[1], curr.Corners[2], btLerp));
                            }
                            else
                            {
                                /* T-T are separated */
                                /* Perform interpolation */
                                var tbLerp = ReverseLerp(curr.Values[3], curr.Values[0], isoLine);
                                var trLerp = ReverseLerp(curr.Values[3], curr.Values[2], isoLine);
                                var tcLerp = ReverseLerp(curr.Values[3], centerVal, isoLine);
                                var blLerp = ReverseLerp(curr.Values[1], curr.Values[0], isoLine);
                                var btLerp = ReverseLerp(curr.Values[1], curr.Values[2], isoLine);
                                var bcLerp = ReverseLerp(curr.Values[1], centerVal, isoLine);
                                builder.AddEdge(Vector2.Lerp(curr.Corners[3], curr.Corners[0], tbLerp),
                                    Vector2.Lerp(curr.Corners[3], center, tcLerp));
                                builder.AddEdge(Vector2.Lerp(curr.Corners[3], center, tcLerp),
                                    Vector2.Lerp(curr.Corners[3], curr.Corners[2], trLerp));
                                builder.AddEdge(Vector2.Lerp(curr.Corners[1], curr.Corners[0], blLerp),
                                    Vector2.Lerp(curr.Corners[1], center, bcLerp));
                                builder.AddEdge(Vector2.Lerp(curr.Corners[1], center, bcLerp),
                                    Vector2.Lerp(curr.Corners[1], curr.Corners[2], btLerp));
                            }
                            break;
                        case 11:        //TTFT
                            /* A polygon */
                            vertLerpAmt = ReverseLerp(curr.Values[1], curr.Values[2], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[3], curr.Values[2], isoLine);
                            vertPoint = Vector2.Lerp(curr.Corners[1], curr.Corners[2], vertLerpAmt);
                            horizPoint = Vector2.Lerp(curr.Corners[3], curr.Corners[2], horizLerpAmt);
                            builder.AddEdge(horizPoint, vertPoint);
                            break;
                        case 12:        //FFTT
                            /* Flattened quad */
                            leftLerpAmt= ReverseLerp(curr.Values[3], curr.Values[0], isoLine);
                            rightLerpAmt = ReverseLerp(curr.Values[2], curr.Values[1], isoLine);
                            builder.AddEdge(Vector2.Lerp(curr.Corners[2], curr.Corners[1], rightLerpAmt),
                                Vector2.Lerp(curr.Corners[3], curr.Corners[0], leftLerpAmt));
                            break;
                        case 13:        //TFTT
                            /* A polygon */
                            vertLerpAmt = ReverseLerp(curr.Values[2], curr.Values[1], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[0], curr.Values[1], isoLine);
                            vertPoint = Vector2.Lerp(curr.Corners[2], curr.Corners[1], vertLerpAmt);
                            horizPoint = Vector2.Lerp(curr.Corners[0], curr.Corners[1], horizLerpAmt);
                            builder.AddEdge(vertPoint, horizPoint);
                            break;
                        case 14:        //FTTT
                            /* A polygon */
                            vertLerpAmt = ReverseLerp(curr.Values[3], curr.Values[0], isoLine);
                            horizLerpAmt = ReverseLerp(curr.Values[1], curr.Values[0], isoLine);
                            vertPoint = Vector2.Lerp(curr.Corners[3], curr.Corners[0], vertLerpAmt);
                            horizPoint = Vector2.Lerp(curr.Corners[1], curr.Corners[0], horizLerpAmt);
                            builder.AddEdge(horizPoint, vertPoint);
                            break;
                        case 15:        //TTTT
                            /* Nothing to do, as there is no contour line here. */
                            break;
                        default:
                            Debug.LogError("Unknown ID value {0..15}");
                            break;
                    }
                }
            }

            return builder;
        }
        
        /// <summary>
        /// Find the parameter (t) which would result in lerping from a to b and arriving at target.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static float ReverseLerp(float a, float b, float target)
        {
            /* If all three args are very close, just return 0 as it won't matter */
            if (Mathf.Abs(a - b) < 0.01f && Mathf.Abs(b - target) < 0.01f)
            {
                return 0.0f;
            }
            if (a > b)
            {
                return 1.0f - ReverseLerp(b, a, target);
            }
            if (target < a || target > b)
            {
                throw new ArgumentException($"target {target} must be between {a} and {b}");
            }
            float badj = b - a;
            float targetadj = target - a;

            return targetadj / badj;
        }
    }
    
    
    
}
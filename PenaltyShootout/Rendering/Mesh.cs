using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace PenaltyShootout.Rendering;

/// <summary>Statická 3D síť (trojúhelníky) uložená na GPU.</summary>
public sealed class Mesh : IDisposable
{
    private readonly VertexBuffer _vertices;
    private readonly IndexBuffer _indices;
    private readonly int _primitiveCount;

    public Mesh(GraphicsDevice gd, List<VertexPositionNormalColor> vertices, List<int> indices)
    {
        _vertices = new VertexBuffer(gd, VertexPositionNormalColor.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
        _vertices.SetData(vertices.ToArray());
        _indices = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
        _indices.SetData(indices.ToArray());
        _primitiveCount = indices.Count / 3;
    }

    public void Draw(GraphicsDevice gd, Effect effect)
    {
        gd.SetVertexBuffer(_vertices);
        gd.Indices = _indices;
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
        }
    }

    public void Dispose()
    {
        _vertices.Dispose();
        _indices.Dispose();
    }
}

/// <summary>Čárová síť (síť brány).</summary>
public sealed class LineMesh : IDisposable
{
    private readonly VertexBuffer _vertices;
    private readonly int _lineCount;

    public LineMesh(GraphicsDevice gd, List<VertexPositionColor> vertices)
    {
        _vertices = new VertexBuffer(gd, VertexPositionColor.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
        _vertices.SetData(vertices.ToArray());
        _lineCount = vertices.Count / 2;
    }

    public void Draw(GraphicsDevice gd, Effect effect)
    {
        gd.SetVertexBuffer(_vertices);
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            gd.DrawPrimitives(PrimitiveType.LineList, 0, _lineCount);
        }
    }

    public void Dispose() => _vertices.Dispose();
}

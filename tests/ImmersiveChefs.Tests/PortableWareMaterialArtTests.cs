using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PortableWareMaterialArtTests
{
    [Test]
    public void Base_stuffable_ware_consumes_its_material_mask()
    {
        var root = FindRepositoryRoot();
        var defs = XDocument.Load(Path.Combine(root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "Kitchenware.xml"));
        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Cookware", "ImmersiveChefs_Plate", "ImmersiveChefs_Cutlery",
                     "ImmersiveChefs_ChefsKnife"
                 })
        {
            var def = defs.Root!.Elements("ThingDef")
                .Single(element => (string?)element.Element("defName") == defName);
            Assert.That((string?)def.Element("graphicData")?.Element("shaderType"), Is.EqualTo("CutoutComplex"),
                defName + " must actually consume its packaged Stuff mask.");
        }
    }

    [TestCase("Plate/Plate")]
    [TestCase("Cutlery/Cutlery")]
    [TestCase("Cookware/Cookware")]
    public void Material_bearing_pixels_are_light_enough_for_core_steel_tint(string relativeAsset)
    {
        var root = FindRepositoryRoot();
        var basePath = Path.Combine(root, "mods", "ImmersiveChefs", "Textures", "ImmersiveChefs", "Things", "Item", "Kitchenware");
        using var diffuse = new Bitmap(Path.Combine(basePath, relativeAsset.Replace('/', Path.DirectorySeparatorChar) + ".png"));
        using var mask = new Bitmap(Path.Combine(basePath, relativeAsset.Replace('/', Path.DirectorySeparatorChar) + "_m.png"));
        var values = new List<double>();
        for (var y = 0; y < diffuse.Height; y++)
        for (var x = 0; x < diffuse.Width; x++)
        {
            var material = mask.GetPixel(x, y);
            var color = diffuse.GetPixel(x, y);
            if (color.A >= 96 && material.R >= 200 && material.G <= 55 && material.B <= 55)
                values.Add(0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B);
        }

        Assert.Multiple(() =>
        {
            Assert.That(values.Count, Is.GreaterThan(128), relativeAsset + " needs a meaningful Stuff-colored region.");
            Assert.That(values.Average(), Is.GreaterThanOrEqualTo(210d),
                relativeAsset + " double-darkens Core steel because its already-dark diffuse is multiplied by Steel's 105/255 Stuff color.");
        });
    }

    [Test]
    public void Modern_cookware_mask_has_no_tiny_fixed_color_islands_inside_material_surfaces()
    {
        var root = FindRepositoryRoot();
        var maskPath = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            "ImmersiveChefs",
            "Things",
            "Item",
            "Kitchenware",
            "Cookware",
            "Cookware_m.png");
        var diffusePath = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            "ImmersiveChefs",
            "Things",
            "Item",
            "Kitchenware",
            "Cookware",
            "Cookware.png");
        using var mask = new Bitmap(maskPath);
        using var diffuse = new Bitmap(diffusePath);
        var approval = XDocument.Load(Path.Combine(root, "docs", "SpriteOutlineApprovals.xml"))
            .Root!
            .Elements("sprite")
            .Single(element => (string?)element.Attribute("path") == "Item/Kitchenware/Cookware/Cookware.png");
        var outlineColor = ColorTranslator.FromHtml(
            (string?)approval.Attribute("outlineColor") ?? "#17130F");
        var tinyInteriorIslands = FindTinyInteriorFixedMaskIslands(mask, diffuse, outlineColor);
        var tinyEnclosedAlphaHoles = FindTinyEnclosedTransparentHoles(mask);

        Assert.Multiple(() =>
        {
            Assert.That(
                tinyInteriorIslands,
                Is.Empty,
                "Tiny fixed-mask islands become untinted pinholes and blocky mip artifacts inside Core Steel surfaces: "
                + string.Join(", ", tinyInteriorIslands.Take(20)));
            Assert.That(
                tinyEnclosedAlphaHoles,
                Is.Empty,
                "Tiny enclosed alpha holes become bright or dark pinholes after map-scale resampling: "
                + string.Join(", ", tinyEnclosedAlphaHoles.Take(20)));
        });
    }

    [Test]
    public void Modern_cookware_alpha_is_opaque_except_for_the_narrow_antialiased_silhouette()
    {
        var root = FindRepositoryRoot();
        var diffusePath = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            "ImmersiveChefs",
            "Things",
            "Item",
            "Kitchenware",
            "Cookware",
            "Cookware.png");
        var maskPath = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            "ImmersiveChefs",
            "Things",
            "Item",
            "Kitchenware",
            "Cookware",
            "Cookware_m.png");
        using var diffuse = new Bitmap(diffusePath);
        using var mask = new Bitmap(maskPath);
        var approval = XDocument.Load(Path.Combine(root, "docs", "SpriteOutlineApprovals.xml"))
            .Root!
            .Elements("sprite")
            .Single(element => (string?)element.Attribute("path") == "Item/Kitchenware/Cookware/Cookware.png");
        var approvedComponentCount = (int)approval.Attribute("foregroundComponents")!;
        var maximumFractionalRatio = 0.12d + Math.Max(0, approvedComponentCount - 1) * 0.01d;
        var protectedRegions = approval.Elements("sourceContourExclusion")
            .Select(element => new Rectangle(
                (int)element.Attribute("x")!,
                (int)element.Attribute("y")!,
                (int)element.Attribute("width")!,
                (int)element.Attribute("height")!))
            .ToArray();
        var visiblePixels = 0;
        var fractionalPixels = 0;
        for (var y = 0; y < diffuse.Height; y++)
        for (var x = 0; x < diffuse.Width; x++)
        {
            var alpha = diffuse.GetPixel(x, y).A;
            if (alpha == 0) continue;
            visiblePixels++;
            if (alpha < 255) fractionalPixels++;
        }
        var interiorFractionalPixels = FindInteriorFractionalAlphaPixels(
            diffuse,
            mask,
            protectedRegions,
            maximumBoundaryDistance: 7,
            outlineColor: ColorTranslator.FromHtml(
                (string?)approval.Attribute("outlineColor") ?? "#17130F"));

        Assert.Multiple(() =>
        {
            Assert.That(visiblePixels, Is.GreaterThan(10_000), "The cookware silhouette is unexpectedly empty.");
            Assert.That(
                fractionalPixels / (double)visiblePixels,
                Is.LessThanOrEqualTo(maximumFractionalRatio),
                $"Fractional alpha exceeds the narrow antialiased perimeter allowance for {approvedComponentCount} approved components; a translucent interior creates a mottled seam against the comic contour.");
            Assert.That(
                interiorFractionalPixels,
                Is.Empty,
                "Fractional alpha is allowed only near a real transparent boundary or the approved semantic gap: "
                + string.Join(", ", interiorFractionalPixels.Take(20)));
        });
    }

    [Test]
    public void Small_interior_fractional_patch_is_rejected_even_when_the_global_ratio_is_low()
    {
        using var diffuse = new Bitmap(31, 31);
        using var mask = new Bitmap(31, 31);
        for (var y = 2; y <= 28; y++)
        for (var x = 2; x <= 28; x++)
        {
            diffuse.SetPixel(x, y, Color.FromArgb(255, 180, 180, 180));
            mask.SetPixel(x, y, Color.FromArgb(255, 255, 0, 0));
        }
        diffuse.SetPixel(15, 15, Color.FromArgb(128, 180, 180, 180));
        mask.SetPixel(15, 15, Color.FromArgb(128, 255, 0, 0));

        Assert.That(
            FindInteriorFractionalAlphaPixels(diffuse, mask, Array.Empty<Rectangle>(), maximumBoundaryDistance: 7),
            Has.Count.EqualTo(1),
            "A small translucent interior patch must not hide behind a passing global alpha ratio.");
    }

    [Test]
    public void Enclosed_alpha_hole_does_not_exempt_an_adjacent_fixed_mask_speck()
    {
        using var mask = new Bitmap(21, 21);
        using var diffuse = new Bitmap(21, 21);
        for (var y = 2; y <= 18; y++)
        for (var x = 2; x <= 18; x++)
        {
            mask.SetPixel(x, y, Color.FromArgb(255, 255, 0, 0));
            diffuse.SetPixel(x, y, Color.FromArgb(255, 220, 220, 220));
        }

        mask.SetPixel(10, 10, Color.Transparent);
        mask.SetPixel(10, 9, Color.FromArgb(255, 0, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(FindTinyInteriorFixedMaskIslands(mask, diffuse), Has.Count.EqualTo(1),
                "Only edge-connected transparency may exempt a fixed-color contour component.");
            Assert.That(FindTinyEnclosedTransparentHoles(mask), Has.Count.EqualTo(1),
                "The enclosed transparent pinhole must be reported independently.");
        });
    }

    [Test]
    public void Dark_diffuse_color_does_not_exempt_an_isolated_fixed_mask_speck()
    {
        using var mask = new Bitmap(21, 21);
        using var diffuse = new Bitmap(21, 21);
        for (var y = 2; y <= 18; y++)
        for (var x = 2; x <= 18; x++)
        {
            mask.SetPixel(x, y, Color.FromArgb(255, 255, 0, 0));
            diffuse.SetPixel(x, y, Color.FromArgb(255, 50, 50, 50));
        }

        mask.SetPixel(10, 10, Color.FromArgb(255, 0, 0, 0));
        diffuse.SetPixel(10, 10, Color.FromArgb(255, 23, 19, 15));

        Assert.That(
            FindTinyInteriorFixedMaskIslands(mask, diffuse),
            Has.Count.EqualTo(1),
            "A black mask defect remains invalid over dark diffuse shading; semantic approval cannot be inferred from color.");
    }

    [Test]
    public void Interior_mask_speck_does_not_borrow_a_diffuse_only_path_to_the_exterior_contour()
    {
        using var mask = new Bitmap(21, 21);
        using var diffuse = new Bitmap(21, 21);
        for (var y = 2; y <= 18; y++)
        for (var x = 2; x <= 18; x++)
        {
            mask.SetPixel(x, y, Color.FromArgb(255, 255, 0, 0));
            diffuse.SetPixel(x, y, Color.FromArgb(255, 180, 180, 180));
        }
        for (var x = 2; x <= 10; x++)
            diffuse.SetPixel(x, 10, Color.FromArgb(255, 23, 19, 15));
        mask.SetPixel(10, 10, Color.FromArgb(255, 0, 0, 0));

        Assert.That(
            FindTinyInteriorFixedMaskIslands(mask, diffuse),
            Has.Count.EqualTo(1),
            "Diffuse outline color alone cannot authorize an interior fixed-black mask component.");
    }

    private static List<string> FindTinyInteriorFixedMaskIslands(
        Bitmap mask,
        Bitmap diffuse,
        Color? outlineColor = null)
    {
        Assert.That(diffuse.Size, Is.EqualTo(mask.Size));
        var visited = new bool[mask.Width, mask.Height];
        var tinyInteriorIslands = new List<string>();
        var exteriorBackground = FindEdgeConnectedTransparentBackground(mask);
        var neighbors = new[] { new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1) };

        for (var y = 0; y < mask.Height; y++)
        for (var x = 0; x < mask.Width; x++)
        {
            if (visited[x, y] || !IsOpaqueFixedMaskPixel(mask.GetPixel(x, y))) continue;

            var pending = new Queue<Point>();
            pending.Enqueue(new Point(x, y));
            visited[x, y] = true;
            var component = new List<Point>();
            var pixelCount = 0;
            var minimumX = x;
            var maximumX = x;
            var minimumY = y;
            var maximumY = y;

            while (pending.Count > 0)
            {
                var point = pending.Dequeue();
                component.Add(point);
                pixelCount++;
                minimumX = Math.Min(minimumX, point.X);
                maximumX = Math.Max(maximumX, point.X);
                minimumY = Math.Min(minimumY, point.Y);
                maximumY = Math.Max(maximumY, point.Y);
                foreach (var offset in neighbors)
                {
                    var neighborX = point.X + offset.X;
                    var neighborY = point.Y + offset.Y;
                    if (neighborX < 0 || neighborY < 0 || neighborX >= mask.Width || neighborY >= mask.Height)
                        continue;

                    var neighbor = mask.GetPixel(neighborX, neighborY);
                    if (visited[neighborX, neighborY] || !IsOpaqueFixedMaskPixel(neighbor)) continue;
                    visited[neighborX, neighborY] = true;
                    pending.Enqueue(new Point(neighborX, neighborY));
                }
            }

            var isApprovedExteriorContour = IsConnectedToExteriorContour(
                component,
                mask,
                diffuse,
                exteriorBackground,
                outlineColor ?? Color.FromArgb(23, 19, 15));
            if (!isApprovedExteriorContour && pixelCount <= 16)
            {
                tinyInteriorIslands.Add($"{pixelCount}px at ({minimumX},{minimumY})-({maximumX},{maximumY})");
            }
        }

        return tinyInteriorIslands;
    }

    private static List<string> FindTinyEnclosedTransparentHoles(Bitmap mask)
    {
        var exteriorBackground = FindEdgeConnectedTransparentBackground(mask);
        var visited = new bool[mask.Width, mask.Height];
        var holes = new List<string>();
        var neighbors = new[] { new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1) };

        for (var y = 0; y < mask.Height; y++)
        for (var x = 0; x < mask.Width; x++)
        {
            if (visited[x, y] || exteriorBackground[x, y] || mask.GetPixel(x, y).A != 0) continue;

            var pending = new Queue<Point>();
            pending.Enqueue(new Point(x, y));
            visited[x, y] = true;
            var component = new List<Point>();
            var pixelCount = 0;
            var minimumX = x;
            var maximumX = x;
            var minimumY = y;
            var maximumY = y;
            while (pending.Count > 0)
            {
                var point = pending.Dequeue();
                component.Add(point);
                pixelCount++;
                minimumX = Math.Min(minimumX, point.X);
                maximumX = Math.Max(maximumX, point.X);
                minimumY = Math.Min(minimumY, point.Y);
                maximumY = Math.Max(maximumY, point.Y);
                foreach (var offset in neighbors)
                {
                    var neighborX = point.X + offset.X;
                    var neighborY = point.Y + offset.Y;
                    if (neighborX < 0 || neighborY < 0 || neighborX >= mask.Width || neighborY >= mask.Height ||
                        visited[neighborX, neighborY] || exteriorBackground[neighborX, neighborY] ||
                        mask.GetPixel(neighborX, neighborY).A != 0) continue;
                    visited[neighborX, neighborY] = true;
                    pending.Enqueue(new Point(neighborX, neighborY));
                }
            }

            var touchesMaterialSurface = component.Any(point => neighbors.Any(offset =>
            {
                var neighborX = point.X + offset.X;
                var neighborY = point.Y + offset.Y;
                return neighborX >= 0 && neighborY >= 0 && neighborX < mask.Width && neighborY < mask.Height &&
                       IsOpaqueMaterialMaskPixel(mask.GetPixel(neighborX, neighborY));
            }));
            if (touchesMaterialSurface && pixelCount <= 16)
                holes.Add($"{pixelCount}px at ({minimumX},{minimumY})-({maximumX},{maximumY})");
        }

        return holes;
    }

    private static bool[,] FindEdgeConnectedTransparentBackground(Bitmap mask)
    {
        var exterior = new bool[mask.Width, mask.Height];
        var pending = new Queue<Point>();
        for (var x = 0; x < mask.Width; x++)
        {
            pending.Enqueue(new Point(x, 0));
            pending.Enqueue(new Point(x, mask.Height - 1));
        }
        for (var y = 1; y < mask.Height - 1; y++)
        {
            pending.Enqueue(new Point(0, y));
            pending.Enqueue(new Point(mask.Width - 1, y));
        }

        var neighbors = new[] { new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1) };
        while (pending.Count > 0)
        {
            var point = pending.Dequeue();
            if (exterior[point.X, point.Y] || mask.GetPixel(point.X, point.Y).A != 0) continue;
            exterior[point.X, point.Y] = true;
            foreach (var offset in neighbors)
            {
                var neighborX = point.X + offset.X;
                var neighborY = point.Y + offset.Y;
                if (neighborX >= 0 && neighborY >= 0 && neighborX < mask.Width && neighborY < mask.Height &&
                    !exterior[neighborX, neighborY])
                    pending.Enqueue(new Point(neighborX, neighborY));
            }
        }

        return exterior;
    }

    private static bool IsConnectedToExteriorContour(
        IEnumerable<Point> component,
        Bitmap mask,
        Bitmap diffuse,
        bool[,] exteriorBackground,
        Color outlineColor)
    {
        var pending = new Queue<Point>(component.Where(point =>
            IsExactOutlinePixel(diffuse.GetPixel(point.X, point.Y), outlineColor) &&
            IsFixedBlackMaskPixel(mask.GetPixel(point.X, point.Y))));
        var visited = new HashSet<Point>(pending);
        var neighbors = new[]
        {
            new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1),
            new Point(1, 1), new Point(1, -1), new Point(-1, 1), new Point(-1, -1),
        };
        while (pending.Count > 0)
        {
            var point = pending.Dequeue();
            foreach (var offset in neighbors)
            {
                var x = point.X + offset.X;
                var y = point.Y + offset.Y;
                if (x < 0 || y < 0 || x >= diffuse.Width || y >= diffuse.Height) continue;
                if (exteriorBackground[x, y]) return true;
                var neighbor = new Point(x, y);
                if (!visited.Contains(neighbor) &&
                    IsExactOutlinePixel(diffuse.GetPixel(x, y), outlineColor) &&
                    IsFixedBlackMaskPixel(mask.GetPixel(x, y)))
                {
                    visited.Add(neighbor);
                    pending.Enqueue(neighbor);
                }
            }
        }
        return false;
    }

    private static bool IsExactOutlinePixel(Color pixel, Color outlineColor) =>
        pixel.A > 0 &&
        pixel.R == outlineColor.R &&
        pixel.G == outlineColor.G &&
        pixel.B == outlineColor.B;

    private static bool IsFixedBlackMaskPixel(Color pixel) =>
        pixel.A > 0 && pixel.R == 0 && pixel.G == 0 && pixel.B == 0;

    private static List<string> FindInteriorFractionalAlphaPixels(
        Bitmap diffuse,
        Bitmap mask,
        IReadOnlyCollection<Rectangle> protectedRegions,
        int maximumBoundaryDistance,
        Color? outlineColor = null)
    {
        Assert.That(mask.Size, Is.EqualTo(diffuse.Size));
        var exteriorBackground = FindEdgeConnectedTransparentBackground(mask);
        var distance = new int[diffuse.Width, diffuse.Height];
        var pending = new Queue<Point>();
        for (var y = 0; y < diffuse.Height; y++)
        for (var x = 0; x < diffuse.Width; x++)
        {
            distance[x, y] = int.MaxValue;
            if (diffuse.GetPixel(x, y).A != 0) continue;
            distance[x, y] = 0;
            pending.Enqueue(new Point(x, y));
        }

        var neighbors = new[]
        {
            new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1),
            new Point(1, 1), new Point(1, -1), new Point(-1, 1), new Point(-1, -1),
        };
        while (pending.Count > 0)
        {
            var point = pending.Dequeue();
            foreach (var offset in neighbors)
            {
                var x = point.X + offset.X;
                var y = point.Y + offset.Y;
                if (x < 0 || y < 0 || x >= diffuse.Width || y >= diffuse.Height ||
                    distance[x, y] <= distance[point.X, point.Y] + 1) continue;
                distance[x, y] = distance[point.X, point.Y] + 1;
                pending.Enqueue(new Point(x, y));
            }
        }

        var approvedContour = new bool[diffuse.Width, diffuse.Height];
        var contourPending = new Queue<Point>();
        for (var y = 0; y < diffuse.Height; y++)
        for (var x = 0; x < diffuse.Width; x++)
        {
            if (!IsExactOutlinePixel(
                    diffuse.GetPixel(x, y),
                    outlineColor ?? Color.FromArgb(23, 19, 15)) ||
                !IsFixedBlackMaskPixel(mask.GetPixel(x, y)) ||
                !neighbors.Any(offset =>
                {
                    var neighborX = x + offset.X;
                    var neighborY = y + offset.Y;
                    return neighborX >= 0 && neighborY >= 0 &&
                           neighborX < diffuse.Width && neighborY < diffuse.Height &&
                           exteriorBackground[neighborX, neighborY];
                })) continue;
            approvedContour[x, y] = true;
            contourPending.Enqueue(new Point(x, y));
        }
        while (contourPending.Count > 0)
        {
            var point = contourPending.Dequeue();
            foreach (var offset in neighbors)
            {
                var x = point.X + offset.X;
                var y = point.Y + offset.Y;
                if (x < 0 || y < 0 || x >= diffuse.Width || y >= diffuse.Height || approvedContour[x, y] ||
                    !IsExactOutlinePixel(
                        diffuse.GetPixel(x, y),
                        outlineColor ?? Color.FromArgb(23, 19, 15)) ||
                    !IsFixedBlackMaskPixel(mask.GetPixel(x, y))) continue;
                approvedContour[x, y] = true;
                contourPending.Enqueue(new Point(x, y));
            }
        }

        var contourDistance = new int[diffuse.Width, diffuse.Height];
        for (var y = 0; y < diffuse.Height; y++)
        for (var x = 0; x < diffuse.Width; x++)
        {
            contourDistance[x, y] = approvedContour[x, y] ? 0 : int.MaxValue;
            if (approvedContour[x, y]) contourPending.Enqueue(new Point(x, y));
        }
        while (contourPending.Count > 0)
        {
            var point = contourPending.Dequeue();
            foreach (var offset in neighbors)
            {
                var x = point.X + offset.X;
                var y = point.Y + offset.Y;
                if (x < 0 || y < 0 || x >= diffuse.Width || y >= diffuse.Height ||
                    contourDistance[x, y] <= contourDistance[point.X, point.Y] + 1) continue;
                contourDistance[x, y] = contourDistance[point.X, point.Y] + 1;
                contourPending.Enqueue(new Point(x, y));
            }
        }

        var permittedGapRegions = protectedRegions.Select(region =>
        {
            region.Inflate(maximumBoundaryDistance, maximumBoundaryDistance);
            return region;
        }).ToArray();
        var result = new List<string>();
        for (var y = 0; y < diffuse.Height; y++)
        for (var x = 0; x < diffuse.Width; x++)
        {
            var alpha = diffuse.GetPixel(x, y).A;
            if (alpha == 0 || alpha == 255 || distance[x, y] <= maximumBoundaryDistance ||
                contourDistance[x, y] <= 2 ||
                permittedGapRegions.Any(region => region.Contains(x, y))) continue;
            result.Add($"alpha {alpha} at ({x},{y}), {distance[x, y]}px from transparency");
        }
        return result;
    }

    private static bool IsOpaqueFixedMaskPixel(Color pixel) =>
        pixel.A >= 96 && pixel.R <= 55 && pixel.G <= 55 && pixel.B <= 55;

    private static bool IsOpaqueMaterialMaskPixel(Color pixel) =>
        pixel.A >= 96 && pixel.R >= 200 && pixel.G <= 55 && pixel.B <= 55;

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}

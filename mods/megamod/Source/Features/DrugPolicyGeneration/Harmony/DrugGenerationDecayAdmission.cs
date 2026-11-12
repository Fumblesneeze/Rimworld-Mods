using System;
using System.Reflection;
using Megamod.Patching;

namespace Megamod.Features.DrugPolicyGeneration;

/// <summary>Current contribution-flow receipt only. It does not admit the called native helper bodies.</summary>
public sealed class DrugGenerationDecayAdmission
{
    private readonly DrugGenerationObservedBodies bodies;
    public DrugGenerationDecayAdmission(MethodInfo target, DrugGenerationDecayGuard guard)
    {
        bodies = new(new[] { new DrugGenerationObservedBodies.Root(target, guard.Validate) });
        string? Validate() => DrugGenerationDecayGuard.Same(target, guard.Target) ? guard.ValidateShape() : "The decay guard is bound to a different target.";
        void Install()
        {
            var failure = Validate(); if (failure != null) throw new InvalidOperationException(failure);
            bodies.Install();
        }
        Candidate = new("DG-DECAY-CONTRIBUTION", "DG-NATIVE-FACTS", target.ToString()!, "C1 decay contribution proof",
            "no generation recommendation", Validate, Install, bodies.Rollback);
    }
    public PatchCandidate Candidate { get; }
    public bool Available => bodies.Available;
    public bool Revalidate() => bodies.Revalidate();
    public string? Failure => bodies.Failure;
}

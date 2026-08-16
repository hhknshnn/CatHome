using NUnit.Framework;
using UnityEngine;

public sealed class CatMovementInputBlockTests
{
    private GameObject cat;
    private CatMovement movement;

    [SetUp]
    public void SetUp()
    {
        cat=new GameObject("InputBlockTestCat",typeof(CharacterController),typeof(CatMovement));
        movement=cat.GetComponent<CatMovement>();
    }

    [TearDown]
    public void TearDown()
    {
        if(cat!=null)Object.DestroyImmediate(cat);
    }

    [Test]
    public void SameOwnerAcquire_IsIdempotent()
    {
        GameObject owner=new GameObject("OnboardingOwner");
        try
        {
            movement.AcquireInputBlock(owner);
            movement.AcquireInputBlock(owner);
            Assert.IsTrue(movement.IsInputBlocked);

            movement.ReleaseInputBlock(owner);
            Assert.IsFalse(movement.IsInputBlocked);
        }
        finally { Object.DestroyImmediate(owner); }
    }

    [Test]
    public void ReleasingOnboardingOwner_DoesNotReleaseModalOwner()
    {
        GameObject onboarding=new GameObject("OnboardingOwner");
        GameObject modal=new GameObject("OfflinePopupOwner");
        try
        {
            movement.AcquireInputBlock(onboarding);
            movement.AcquireInputBlock(modal);
            movement.ReleaseInputBlock(onboarding);
            Assert.IsTrue(movement.IsInputBlocked);

            movement.ReleaseInputBlock(modal);
            Assert.IsFalse(movement.IsInputBlocked);
        }
        finally
        {
            Object.DestroyImmediate(onboarding);
            Object.DestroyImmediate(modal);
        }
    }

    [Test]
    public void DestroyedOwner_IsPrunedBeforeQuery()
    {
        GameObject owner=new GameObject("DestroyedOwner");
        movement.AcquireInputBlock(owner);
        Object.DestroyImmediate(owner);

        Assert.IsFalse(movement.IsInputBlocked);
        Assert.IsFalse(movement.IsMovementLocked);
    }

    [Test]
    public void CategoryPolicy_OnlyBlocksRequestedInputs()
    {
        GameObject owner=new GameObject("PetTutorialOwner");
        try
        {
            movement.SetInputBlock(owner,CatInputCategory.Movement|CatInputCategory.WorldActions);

            Assert.IsTrue(movement.IsMovementLocked);
            Assert.IsTrue(movement.AreWorldActionsBlocked);
            Assert.IsFalse(movement.IsPettingInputBlocked);

            movement.SetInputBlock(owner,CatInputCategory.Petting|CatInputCategory.WorldActions);

            Assert.IsFalse(movement.IsMovementLocked);
            Assert.IsTrue(movement.AreWorldActionsBlocked);
            Assert.IsTrue(movement.IsPettingInputBlocked);
        }
        finally { Object.DestroyImmediate(owner); }
    }

    [Test]
    public void ReplacingOneOwnersPolicy_DoesNotAffectAnotherOwner()
    {
        GameObject onboarding=new GameObject("OnboardingOwner");
        GameObject modal=new GameObject("ModalOwner");
        try
        {
            movement.SetInputBlock(onboarding,CatInputCategory.Petting);
            movement.AcquireInputBlock(modal);
            movement.ReleaseInputBlock(onboarding);

            Assert.IsTrue(movement.IsMovementLocked);
            Assert.IsTrue(movement.IsPettingInputBlocked);
            Assert.IsTrue(movement.AreWorldActionsBlocked);
        }
        finally
        {
            Object.DestroyImmediate(onboarding);
            Object.DestroyImmediate(modal);
        }
    }
}

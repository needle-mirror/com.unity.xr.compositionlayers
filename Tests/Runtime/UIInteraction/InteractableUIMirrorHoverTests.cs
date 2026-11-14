#if UNITY_XR_INTERACTION_TOOLKIT
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Unity.XR.CompositionLayers.Tests.UIInteraction
{
    class InteractableUIMirrorHoverTests : UIPanelTestBase
    {
        [Test]
        public async Task ForcedHover_EnablesCanvasRaycasts()
        {
            var panel = CreatePanel();
            await Awaitable.NextFrameAsync();

            var interactor = CreateInteractor(panel);
            Assert.IsFalse(CanvasBlocksRaycasts(panel));

            ForceHover(interactor, panel);

            Assert.IsTrue(GetInteractable(panel).isHovered);
            Assert.IsTrue(CanvasBlocksRaycasts(panel));
        }

        [Test]
        public async Task HoverExit_EndsAndStopsForwardingProxyUIHover()
        {
            var panel = CreatePanel();
            await Awaitable.NextFrameAsync();

            var interactor = CreateInteractor(panel);
            var uiHoverEnteredCount = 0;
            var uiHoverExitedCount = 0;
            interactor.uiHoverEntered.AddListener(_ => uiHoverEnteredCount++);
            interactor.uiHoverExited.AddListener(_ => uiHoverExitedCount++);

            ForceHover(interactor, panel);
            IUIHoverInteractor proxy = FindProxyInteractor(interactor);
            var proxyUIHoverArgs = new UIHoverEventArgs { interactorObject = proxy };

            proxy.OnUIHoverEntered(proxyUIHoverArgs);
            Assert.AreEqual(1, uiHoverEnteredCount);

            // The proxy was still hovering UI, so the real interactor must be told the hover ended.
            ForceHoverExit(interactor, panel);
            Assert.AreEqual(1, uiHoverExitedCount);

            proxy.OnUIHoverEntered(proxyUIHoverArgs);
            Assert.AreEqual(1, uiHoverEnteredCount);
        }
    }
}
#endif

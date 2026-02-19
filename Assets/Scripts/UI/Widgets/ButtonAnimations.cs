using System;
using FastSpring;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace SiegeSurvival
{
    public class ButtonAnimations : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        TransformSpring _spring;
        [SerializeField] private AudioClip _clickClip, _enterClip, _exitClip;

        void Awake()
        {
            _spring = GetComponentInChildren<TransformSpring>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_spring.IsAtEquilibrium)
                return;
            _spring.BumpRotation(Random.Range(.1f, .3f) * RandomSign())
                .BumpScale(3f);

            // if (_enterClip != null)
            //     SfxPlayer.Play(_enterClip, 0.5f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_spring.IsAtEquilibrium)
                return;
            
            _spring.BumpScale(2f);

            // if (_exitClip != null)
            //     SfxPlayer.Play(_exitClip, 0.5f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _spring.BumpRotation(Random.Range(0.5f, 1f) * RandomSign())
                .BumpScale(7);

            // if (_clickClip != null)
            //     SfxPlayer.Play(_clickClip);
        }

        private static int RandomSign()
        {
            return Random.value > 0.5f ? 1 : -1;
        }
   }
}

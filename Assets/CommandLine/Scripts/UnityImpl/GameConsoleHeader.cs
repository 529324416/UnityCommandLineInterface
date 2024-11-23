using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace RedSaw.CommandLineInterface{

    [RequireComponent(typeof(Image))]
    public class GameConsoleHeader : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
    {
        [SerializeField] private Color hoverColor = Color.white;
        private Image headerImage;
        private Color normalColor;
        private RectTransform movTarget;
        private Vector2 movPos;
        // private Action<Vector2> movCb;

        bool shouldMov;
        Vector2 startPos;

        public void Init(RectTransform target){

            this.movTarget = target;
            headerImage = GetComponent<Image>();
            if( headerImage == null ){
                throw new System.Exception("GameConsoleHeader must attach to a GameObject with Image component");
            }
            normalColor = headerImage.color;
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            headerImage.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            headerImage.color = normalColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            shouldMov = true;
            startPos = eventData.position;
            movPos = movTarget.position;
        }
        public void OnPointerMove(PointerEventData eventData)
        {
            if(shouldMov){
                Vector2 offset = eventData.position - startPos;
                movTarget.position = movPos + offset;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            shouldMov = false;
        }
    }
}
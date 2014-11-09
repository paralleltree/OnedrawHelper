using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace OnedrawHelper.Behaviors
{
    sealed class BeginDragBehavior : Behavior<FrameworkElement>
    {
        private Point _origin;
        private bool _isButtonDown;

        public DragDropEffects AllowedEffects
        {
            get { return (DragDropEffects)GetValue(AllowedEffectsProperty); }
            set { SetValue(AllowedEffectsProperty, value); }
        }

        public static readonly DependencyProperty AllowedEffectsProperty =
            DependencyProperty.Register("AllowedEffects", typeof(DragDropEffects), typeof(BeginDragBehavior), new PropertyMetadata(DragDropEffects.All));


        public object DragDropData
        {
            get { return (object)GetValue(DragDropDataProperty); }
            set { SetValue(DragDropDataProperty, value); }
        }

        public static readonly DependencyProperty DragDropDataProperty =
            DependencyProperty.Register("DragDropData", typeof(object), typeof(BeginDragBehavior), new PropertyMetadata(null));


        protected override void OnAttached()
        {
            this.AssociatedObject.PreviewMouseDown += AssociatedObject_PreviewMouseDown;
            this.AssociatedObject.PreviewMouseMove += AssociatedObject_PreviewMouseMove;
            this.AssociatedObject.PreviewMouseUp += AssociatedObject_PreviewMouseUp;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.PreviewMouseDown -= AssociatedObject_PreviewMouseDown;
            this.AssociatedObject.PreviewMouseMove -= AssociatedObject_PreviewMouseMove;
            this.AssociatedObject.PreviewMouseUp -= AssociatedObject_PreviewMouseUp;
            base.OnDetaching();
        }


        void AssociatedObject_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _origin = e.GetPosition(this.AssociatedObject);
            _isButtonDown = true;
        }

        void AssociatedObject_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !_isButtonDown)
                return;

            var point = e.GetPosition(this.AssociatedObject);
            if (CheckDistance(point, _origin))
            {
                DragDrop.DoDragDrop(this.AssociatedObject, this.DragDropData, this.AllowedEffects);
                _isButtonDown = false;
                e.Handled = true;
            }
        }

        void AssociatedObject_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isButtonDown = false;
        }


        static bool CheckDistance(Point a, Point b)
        {
            return Math.Abs(a.X - b.Y) >= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(a.Y - b.Y) >= SystemParameters.MinimumVerticalDragDistance;
        }
    }
}

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
    sealed class AcceptDropBehavior : Behavior<FrameworkElement>
    {
        public AcceptDropDescription Description
        {
            get { return (AcceptDropDescription)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(AcceptDropDescription),
            typeof(AcceptDropBehavior), new PropertyMetadata(null));


        protected override void OnAttached()
        {
            this.AssociatedObject.PreviewDragOver += AssociatedObject_DragOver;
            this.AssociatedObject.PreviewDrop += AssociatedObject_Drop;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.PreviewDragOver -= AssociatedObject_DragOver;
            this.AssociatedObject.PreviewDrop -= AssociatedObject_Drop;
            base.OnDetaching();
        }


        void AssociatedObject_DragOver(object sender, DragEventArgs e)
        {
            var desc = Description;
            if (desc == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            desc.OnDragOver(e);
            e.Handled = true;
        }

        void AssociatedObject_Drop(object sender, DragEventArgs e)
        {
            var desc = Description;
            if (desc == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            desc.OnDrop(e);
            e.Handled = true;
        }
    }

    public sealed class AcceptDropDescription
    {
        public event Action<DragEventArgs> DragOver;

        public void OnDragOver(DragEventArgs e)
        {
            var handler = DragOver;
            if (handler != null) handler(e);
        }


        public event Action<DragEventArgs> DragDrop;

        public void OnDrop(DragEventArgs e)
        {
            var handler = DragDrop;
            if (handler != null) handler(e);
        }
    }
}

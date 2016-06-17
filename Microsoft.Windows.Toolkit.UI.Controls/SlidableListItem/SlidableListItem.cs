﻿using System;
using System.Windows.Input;
using Windows.Devices.Input;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Microsoft.Windows.Toolkit.UI.Controls
{
    /// <summary>
    /// ContentControl providing functionality for sliding left or right to expose functions
    /// </summary>
    [TemplatePart(Name = PartContentGrid, Type = typeof(Grid))]
    [TemplatePart(Name = PartCommandContainer, Type = typeof(Grid))]
    [TemplatePart(Name = PartLeftCommandPanel, Type = typeof(StackPanel))]
    [TemplatePart(Name = PartRightCommandPanel, Type = typeof(StackPanel))]
    public class SlidableListItem : ContentControl
    {
        /// <summary>
        /// Indetifies the <see cref="ActivationWidth"/> property
        /// </summary>
        public static readonly DependencyProperty ActivationWidthProperty =
            DependencyProperty.Register("ActivationWidth", typeof(double), typeof(SlidableListItem), new PropertyMetadata(80));

        /// <summary>
        /// Indeifies the <see cref="LeftIcon"/> property
        /// </summary>
        public static readonly DependencyProperty LeftIconProperty =
            DependencyProperty.Register("LeftIcon", typeof(Symbol), typeof(SlidableListItem), new PropertyMetadata(Symbol.Favorite));

        /// <summary>
        /// Indetifies the <see cref="RightIcon"/> property
        /// </summary>
        public static readonly DependencyProperty RightIconProperty =
            DependencyProperty.Register("RightIcon", typeof(Symbol), typeof(SlidableListItem), new PropertyMetadata(Symbol.Delete));

        /// <summary>
        /// Indetifies the <see cref="LeftLabel"/> property
        /// </summary>
        public static readonly DependencyProperty LeftLabelProperty =
            DependencyProperty.Register("LeftLabel", typeof(string), typeof(SlidableListItem), new PropertyMetadata(""));

        /// <summary>
        /// Indetifies the <see cref="RightLabel"/> property
        /// </summary>
        public static readonly DependencyProperty RightLabelProperty =
            DependencyProperty.Register("RightLabel", typeof(string), typeof(SlidableListItem), new PropertyMetadata(""));

        /// <summary>
        /// Indetifies the <see cref="LeftForeground"/> property
        /// </summary>
        public static readonly DependencyProperty LeftForegroundProperty =
            DependencyProperty.Register("LeftForeground", typeof(Brush), typeof(SlidableListItem), new PropertyMetadata(new SolidColorBrush(Colors.White)));

        /// <summary>
        /// Indetifies the <see cref="RightForeground"/> property
        /// </summary>
        public static readonly DependencyProperty RightForegroundProperty =
            DependencyProperty.Register("RightForeground", typeof(Brush), typeof(SlidableListItem), new PropertyMetadata(new SolidColorBrush(Colors.White)));

        /// <summary>
        /// Indetifies the <see cref="LeftBackground"/> property
        /// </summary>
        public static readonly DependencyProperty LeftBackgroundProperty =
            DependencyProperty.Register("LeftBackground", typeof(Brush), typeof(SlidableListItem), new PropertyMetadata(new SolidColorBrush(Colors.LightGray)));

        /// <summary>
        /// Identifies the <see cref="RightBackground"/> property
        /// </summary>
        public static readonly DependencyProperty RightBackgroundProperty =
            DependencyProperty.Register("RightBackground", typeof(Brush), typeof(SlidableListItem), new PropertyMetadata(new SolidColorBrush(Colors.DarkGray)));

        /// <summary>
        /// Identifies the <see cref="MouseSlidingEnabled"/> property
        /// </summary>
        public static readonly DependencyProperty MouseSlidingEnabledProperty =
            DependencyProperty.Register("MouseSlidingEnabled", typeof(bool), typeof(SlidableListItem), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="LeftCommand"/> property
        /// </summary>
        public static readonly DependencyProperty LeftCommandProperty =
            DependencyProperty.Register("LeftCommand", typeof(ICommand), typeof(SlidableListItem), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="RightCommand"/> property
        /// </summary>
        public static readonly DependencyProperty RightCommandProperty =
            DependencyProperty.Register("RightCommand", typeof(ICommand), typeof(SlidableListItem), new PropertyMetadata(null));

        const string PartContentGrid = "ContentGrid";
        const string PartCommandContainer = "CommandContainer";
        const string PartLeftCommandPanel = "LeftCommandPanel";
        const string PartRightCommandPanel = "RightCommandPanel";

        // Content Container
        private Grid _contentGrid;

        // transform for sliding content
        private CompositeTransform _transform;

        // container for command content
        private Grid _commandContainer;

        // container for left command content
        private StackPanel _leftCommandPanel;

        // transform for left command content
        private CompositeTransform _leftCommandTransform;

        // container for right command content
        private StackPanel _rightCommandPanel;

        // transform for right command content
        private CompositeTransform _rightCommandTransform;

        // doubleanimation for snaping back to default position
        private DoubleAnimation _contentAnimation;

        // storyboard for snaping back to default position
        private Storyboard _contentStoryboard;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlidableListItem"/> class.
        /// Creates a new instance of <see cref="SlidableListItem"/>
        /// </summary>
        public SlidableListItem()
        {
            this.DefaultStyleKey = typeof(SlidableListItem);
        }

        /// <summary>
        /// Occurs when the user swipes to the left to activate the right action
        /// </summary>
        public event EventHandler RightCommandRequested;

        /// <summary>
        /// Occurs when the user swipes to the right to activate the left action
        /// </summary>
        public event EventHandler LeftCommandRequested;

        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuilding
        /// layout pass) call <see cref="OnApplyTemplate"/>. In simplest terms, this means the method
        /// is called just before a UI element displays in an application. Override this
        /// method to influence the default post-template logic of a class.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            if (_contentGrid != null)
            {
                _contentGrid.ManipulationDelta -= ContentGrid_ManipulationDelta;
                _contentGrid.ManipulationCompleted += ContentGrid_ManipulationCompleted;
            }

            _contentGrid = this.GetTemplateChild(PartContentGrid) as Grid;
            _commandContainer = this.GetTemplateChild(PartCommandContainer) as Grid;
            _leftCommandPanel = this.GetTemplateChild(PartLeftCommandPanel) as StackPanel;
            _rightCommandPanel = this.GetTemplateChild(PartRightCommandPanel) as StackPanel;

            if (_contentGrid != null)
            {
                _transform = _contentGrid.RenderTransform as CompositeTransform;
                _contentGrid.ManipulationDelta += ContentGrid_ManipulationDelta;
                _contentGrid.ManipulationCompleted += ContentGrid_ManipulationCompleted;

                _contentAnimation = new DoubleAnimation();
                Storyboard.SetTarget(_contentAnimation, _transform);
                Storyboard.SetTargetProperty(_contentAnimation, "TranslateX");
                _contentAnimation.To = 0;
                _contentAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(100));

                _contentStoryboard = new Storyboard();
                _contentStoryboard.Children.Add(_contentAnimation);
            }

            if (_commandContainer != null)
            {
                _commandContainer.Background = LeftBackground as SolidColorBrush;
            }

            if (_leftCommandPanel != null)
            {
                _leftCommandTransform = _leftCommandPanel.RenderTransform as CompositeTransform;
            }

            if (_rightCommandPanel != null)
            {
                _rightCommandTransform = _rightCommandPanel.RenderTransform as CompositeTransform;
            }

            base.OnApplyTemplate();
        }

        /// <summary>
        /// Handler for when slide manipulation is complete
        /// </summary>
        private void ContentGrid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (!MouseSlidingEnabled && e.PointerDeviceType == PointerDeviceType.Mouse)
                return;

            var x = _transform.TranslateX;
            _contentAnimation.From = x;
            _contentStoryboard.Begin();

            _leftCommandTransform.TranslateX = 0;
            _rightCommandTransform.TranslateX = 0;
            _leftCommandPanel.Opacity = 1;
            _rightCommandPanel.Opacity = 1;

            if (x < -ActivationWidth)
            {
                RightCommandRequested?.Invoke(this, new EventArgs());
                RightCommand?.Execute(null);
            }
            else if (x > ActivationWidth)
            {
                LeftCommandRequested?.Invoke(this, new EventArgs());
                LeftCommand?.Execute(null);
            }
        }

        /// <summary>
        /// Handler for when slide manipulation is underway
        /// </summary>
        private void ContentGrid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {

            if (!MouseSlidingEnabled && e.PointerDeviceType == PointerDeviceType.Mouse)
                return;

            _transform.TranslateX += e.Delta.Translation.X;
            var abs = Math.Abs(_transform.TranslateX);

            if (_transform.TranslateX > 0)
            {
                if (_commandContainer != null)
                {
                    _commandContainer.Background = LeftBackground as SolidColorBrush;
                }

                _leftCommandPanel.Opacity = 1;
                _rightCommandPanel.Opacity = 0;

                if (abs < ActivationWidth)
                    _leftCommandTransform.TranslateX = _transform.TranslateX / 2;
                else
                    _leftCommandTransform.TranslateX = 20;
            }
            else
            {
                if (_commandContainer != null)
                {
                    _commandContainer.Background = RightBackground as SolidColorBrush;
                }

                _rightCommandPanel.Opacity = 1;
                _leftCommandPanel.Opacity = 0;

                if (abs < ActivationWidth)
                    _rightCommandTransform.TranslateX = _transform.TranslateX / 2;
                else
                    _rightCommandTransform.TranslateX = -20;
            }

        }

        /// <summary>
        /// Gets or sets the amount of pixels the content needs to be swiped for an 
        /// action to be requested
        /// </summary>
        public double ActivationWidth
        {
            get { return (double)GetValue(ActivationWidthProperty); }
            set { SetValue(ActivationWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the left icon symbol
        /// </summary>
        public Symbol LeftIcon
        {
            get { return (Symbol)GetValue(LeftIconProperty); }
            set { SetValue(LeftIconProperty, value); }
        }

        /// <summary>
        /// Gets or sets the right icon symbol
        /// </summary>
        public Symbol RightIcon
        {
            get { return (Symbol)GetValue(RightIconProperty); }
            set { SetValue(RightIconProperty, value); }
        }

        /// <summary>
        /// Gets or sets the left label
        /// </summary>
        public string LeftLabel
        {
            get { return (string)GetValue(LeftLabelProperty); }
            set { SetValue(LeftLabelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the right label
        /// </summary>
        public string RightLabel
        {
            get { return (string)GetValue(RightLabelProperty); }
            set { SetValue(RightLabelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the left foreground color
        /// </summary>
        public Brush LeftForeground
        {
            get { return (Brush)GetValue(LeftForegroundProperty); }
            set { SetValue(LeftForegroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the right foreground color
        /// </summary>
        public Brush RightForeground
        {
            get { return (Brush)GetValue(RightForegroundProperty); }
            set { SetValue(RightForegroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the left background color
        /// </summary>
        public Brush LeftBackground
        {
            get { return (Brush)GetValue(LeftBackgroundProperty); }
            set { SetValue(LeftBackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the right background color
        /// </summary>
        public Brush RightBackground
        {
            get { return (Brush)GetValue(RightBackgroundProperty); }
            set { SetValue(RightBackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ability to slide the control with the mouse. False by default
        /// </summary>
        public bool MouseSlidingEnabled
        {
            get { return (bool)GetValue(MouseSlidingEnabledProperty); }
            set { SetValue(MouseSlidingEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ICommand for left command request
        /// </summary>
        public ICommand LeftCommand
        {
            get { return (ICommand)GetValue(LeftCommandProperty); }
            set
            {
                SetValue(LeftCommandProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the ICommand for right command request
        /// </summary>
        public ICommand RightCommand
        {
            get { return (ICommand)GetValue(RightCommandProperty); }
            set
            {
                //if (value != null)
                //{
                //    value.CanExecuteChanged += RightCommand_CanExecuteChanged;
                //}
                SetValue(RightCommandProperty, value);
            }
        }

    }
}

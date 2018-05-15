﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing.Text;
using System.Collections.ObjectModel;

namespace Client
{


    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ChatWindow : Window
    {
        bool manualClose = true;
        bool initialized = false;

        private ChatMode chatMode;
        private User targetUser;
        public ChatMode ChatModeP { get => chatMode; private set => chatMode = value; }
        public User TargetUser { get => targetUser; private set => targetUser = value; }

        int fontSize = 12;
        bool isBold = false;
        bool isItalic = false;
        bool isUnderLine = false;
        string fontFamily = "Microsoft YaHei UI";
        string fontColor = "#FF000000";

        public User user = ((App)Application.Current).CurrentUser;
        
        ClientConnector connector = ((App)Application.Current).connector;
        ObservableCollection<User> userList = new ObservableCollection<User>();
        Properties.Settings settings = Properties.Settings.Default;
        SolidColorBrush labelCheckedBrush = new SolidColorBrush(Colors.LightGray);
        
        public ChatWindow()
        {
            Initialize(ChatMode.Group);
        }

        public ChatWindow(User target)
        {
            TargetUser = target;
            Initialize(ChatMode.Private);
        }
        
        private void Initialize(ChatMode mode)
        {
            InitializeComponent();
            connector.ServerDisconnectEvent += Connector_ServerDisconnectEvent;

            ChatModeP = mode;
            switch (mode)
            {
                case ChatMode.Group:
                    {
                        connector.GroupMessageEvent += Connector_GroupMessageEvent;
                        connector.UserJoinEvent += Connector_UserJoinEvent;
                        connector.UserQuitEvent += Connector_UserQuitEvent;
                        userListLV.ItemsSource = userList;
                        userList.Add(user);
                        UpdateTitle();
                        break;
                    }
                case ChatMode.Private:
                    {
                        connector.PrivateMessageEvent += Connector_PrivateMessageEvent;
                        leftGrid.Visibility = Visibility.Collapsed;
                        centerGS.Visibility = Visibility.Collapsed;
                        Title = TargetUser.ToString();
                        break;
                    }
            }

            InstalledFontCollection installedFonts = new InstalledFontCollection();
            FontFamily defaultFont = FontFamily;
            for (int i = 0; i < installedFonts.Families.Length; i++)
            {
                fontFamilyCB.Items.Add(installedFonts.Families[i].Name);
                if (defaultFont.ToString().Equals(installedFonts.Families[i].Name))
                    fontFamilyCB.SelectedIndex = i;
            }
            LoadStyle();
            initialized = true;
        }

        private void LoadStyle()
        {
            FontSizeSwitch(settings.fontSize);
            for (int i = 0; i < fontSizeCB.Items.Count; i++)
                if (settings.fontSize == int.Parse((string)((ComboBoxItem)fontSizeCB.Items[i]).Content))
                    fontSizeCB.SelectedIndex = i;
            FontFamilySwitch(settings.fontFamily);
            for (int i = 0; i < fontFamilyCB.Items.Count; i++)
                if (settings.fontFamily.Equals(fontFamilyCB.Items[i]))
                    fontFamilyCB.SelectedIndex = i;
            BoldSwitch(settings.isBold);
            ItalicSwitch(settings.isItalic);
            UnderLineSwitch(settings.isUnderLIne);
            ColorSwitch((Color)ColorConverter.ConvertFromString(settings.fontColor));
        }

        private void UpdateTitle()
        {
            Title = "群聊室 （目前在线 " + userList.Count + " 人），目前登录：" + user.ToString();
        }

        private void SaveSettings()
        {
            settings.fontSize = fontSize;
            settings.fontFamily = fontFamily;
            settings.fontColor = fontColor;
            settings.isBold = isBold;
            settings.isItalic = isItalic;
            settings.isUnderLIne = isUnderLine;
            settings.Save();
        }
        
        private void SendMessage()
        {
            int style = 0;
            if (isBold) style += 1;
            if (isItalic) style += 10;
            if (isUnderLine) style += 100;

            MessageDictionary message = new MessageDictionary();
            message.Add(MesKeyStr.MessageType, MessageType.Text.ToString());
            message.Add(MesKeyStr.UserID, user.UserID);
            message.Add(MesKeyStr.Content, contentTB.Text);
            message.Add(MesKeyStr.FontFamily, (string)fontFamilyCB.SelectedItem);
            message.Add(MesKeyStr.FontSize, ((ComboBoxItem)fontSizeCB.SelectedItem).Content.ToString());
            message.Add(MesKeyStr.FontStyle, style.ToString());
            message.Add(MesKeyStr.FontColor, fontColor);

            if(chatMode==ChatMode.Group)
                connector.SendGroupMessage(message);
            else
            {
                message.Add(MesKeyStr.TargetUserID, TargetUser.UserID);
                connector.SendPrivateMessage(message);
            }
            //MessageUC messageUC = new MessageUC(message, Sender.self);
            //AddChildToMesListSP(messageUC);
            contentTB.Text = "";
        }

        //关于其他窗口事件的事件处理方法
        #region
        private void sendB_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //MessageBox.Show(this,"想要退出还是注销？",MessageBoxButton.YesNoCancel)
            SaveSettings();
            if (manualClose && chatMode == ChatMode.Group)
            {
                connector.Close();
            }
        }

        private void contentTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
                SendMessage();
        }

        private void logoutB_Click(object sender, RoutedEventArgs e)
        {
            connector.Logout();
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            manualClose = false;
            Close();
        }

        private void userListLV_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            User target = (User)userListLV.SelectedItem;
            ChatWindow chatWindow = new ChatWindow(target);
            chatWindow.Show();
        }

        private void PrivateChatMI_Click(object sender, RoutedEventArgs e)
        {
            User target = (User)userListLV.SelectedItem;
            ChatWindow chatWindow = new ChatWindow(target);
            chatWindow.Show();
        }
        #endregion

        //关于字体样式的方法
        #region
        private void fontFamilyCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FontFamilySwitch((string)fontFamilyCB.SelectedItem);
        }

        private void FontFamilySwitch(string font)
        {
            FontFamilyConverter fontFamilyConverter = new FontFamilyConverter();
            try
            {
                contentTB.FontFamily = (FontFamily)fontFamilyConverter.ConvertFromString(font);
                fontFamily = font;
            }
            catch (Exception ex)
            {
                MessageBox.Show("字体" + fontFamily + "不存在！\n" + ex.Message + ex.StackTrace);
            }
        }

        private void fontSizeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initialized)
                FontSizeSwitch(int.Parse(((ComboBoxItem)fontSizeCB.SelectedItem).Content.ToString()));
        }

        private void FontSizeSwitch(int size)
        {
            contentTB.FontSize = size;
            fontSize = size;
        }

        private void boldL_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BoldSwitch(!isBold);
        }

        private void BoldSwitch(bool bold)
        {
            if (bold)
            {
                isBold = true;
                boldL.FontWeight = FontWeights.Bold;
                boldL.Background = labelCheckedBrush;
                contentTB.FontWeight = FontWeights.Bold;
            }
            else
            {
                isBold = false;
                boldL.FontWeight = FontWeights.Normal;
                boldL.Background = null;
                contentTB.FontWeight = FontWeights.Normal;
            }

        }

        private void italicL_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ItalicSwitch(!isItalic);
        }

        private void ItalicSwitch(bool italic)
        {
            if (italic)
            {
                isItalic = true;
                italicL.FontStyle = FontStyles.Italic;
                italicL.Background = labelCheckedBrush;
                contentTB.FontStyle = FontStyles.Italic;
            }
            else
            {
                isItalic = false;
                italicL.FontStyle = FontStyles.Normal;
                italicL.Background = null;
                contentTB.FontStyle = FontStyles.Normal;
            }
        }

        private void underLineL_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UnderLineSwitch(!isUnderLine);
        }

        private void UnderLineSwitch(bool underLine)
        {
            if (underLine)
            {
                isUnderLine = true;
                underLineL.Background = labelCheckedBrush;
                contentTB.TextDecorations = TextDecorations.Underline;

            }
            else
            {
                isUnderLine = false;
                underLineL.Background = null;
                contentTB.TextDecorations = null;
            }
        }

        private void fontColorL_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Color color;
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                color = Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                fontColor = color.ToString();
                ColorSwitch(color);
            }
        }

        private void ColorSwitch(Color color)
        {
            SolidColorBrush solidColorBrush = new SolidColorBrush(color);
            contentTB.Foreground = solidColorBrush;
            fontColorL.Foreground = solidColorBrush;
        }
        #endregion

        //关于connector的事件处理方法
        #region

        private void AddChildToMesListSP(UIElement element)
        {
            if (messageListSP.Children.Count == 500)
                messageListSP.Children.RemoveAt(0);
            messageListSP.Children.Add(element);
            messageListSV.ScrollToEnd();
        }

        private void Connector_ServerDisconnectEvent(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (chatMode == ChatMode.Group)
                {
                    MessageBox.Show(this, "服务器关闭或失去连接，请重新登录。");
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    manualClose = false;
                }
                Close();
            });
        }

        private void Connector_GroupMessageEvent(object sender, MessageDictionary e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Sender s;
                if (user.UserID.Equals(e[MesKeyStr.UserID]))
                    s = Sender.self;
                else
                    s = Sender.others;
                MessageUC messageUC = new MessageUC(e, s);
                AddChildToMesListSP(messageUC);
            });
        }

        private void Connector_PrivateMessageEvent(object sender, MessageDictionary e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Sender s;
                if (user.UserID.Equals(e[MesKeyStr.UserID]))
                    s = Sender.self;
                else
                    s = Sender.others;
                MessageUC messageUC = new MessageUC(e, s);
                AddChildToMesListSP(messageUC);
            });
        }

        private void Connector_UserJoinEvent(object sender, User e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                userList.Add(e);
                UpdateTitle();

                Label label = new Label();
                label.Content = "用户 " + e.ToString() + " 加入群聊";
                label.HorizontalAlignment = HorizontalAlignment.Center;
                AddChildToMesListSP(label);

            });
        }

        private void Connector_UserQuitEvent(object sender, User e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                userList.Remove(e);
                UpdateTitle();

                Label label = new Label();
                label.Content = "用户 " + e.ToString() + " 退出群聊";
                label.HorizontalAlignment = HorizontalAlignment.Center;
                AddChildToMesListSP(label);
            });
        }

        private void Connector_TargetQuitEvent(object sender, User e)
        {
            if(e.Equals(TargetUser))
            {
                if (MessageBox.Show("对方已离线，是否关闭本窗口？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    Close();
                else
                    sendB.IsEnabled = false;
            }
            return;
        }
        #endregion

    }
}

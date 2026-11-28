using System;
using System.Collections.Generic;
using System.Globalization;

namespace WhosListening
{
    public static class Strings
    {
        public static readonly string[] Languages = new string[] { "es", "en", "pt", "fr", "de" };

        private static readonly Dictionary<string, Dictionary<string, string>> tables =
            new Dictionary<string, Dictionary<string, string>>();
        private static string current;
        private static readonly object sync = new object();

        static Strings()
        {
            tables["es"] = BuildEs();
            tables["en"] = BuildEn();
            tables["pt"] = BuildPt();
            tables["fr"] = BuildFr();
            tables["de"] = BuildDe();
        }

        public static string Current
        {
            get
            {
                lock (sync)
                {
                    if (current == null) current = Normalize(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
                    return current;
                }
            }
        }

        public static void SetLanguage(string twoLetter)
        {
            lock (sync)
            {
                current = Normalize(twoLetter);
            }
        }

        private static string Normalize(string twoLetter)
        {
            if (twoLetter == null) return "en";
            string l = twoLetter.Trim().ToLowerInvariant();
            if (Array.IndexOf(Languages, l) >= 0) return l;
            return "en";
        }

        public static string Get(string key)
        {
            string value;
            if (tables[Current].TryGetValue(key, out value)) return value;
            if (tables["en"].TryGetValue(key, out value)) return value;
            return key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, Get(key), args);
        }

        public static IEnumerable<string> KeysFor(string lang)
        {
            Dictionary<string, string> table;
            if (lang != null && tables.TryGetValue(lang, out table)) return new List<string>(table.Keys);
            return new List<string>();
        }

        private static Dictionary<string, string> BuildEs()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d["title"] = "¿Quién está escuchando?";
            d["inUse"] = "En uso";
            d["addAccount"] = "Agregar cuenta";
            d["addSaveCurrent"] = "Guardar la cuenta actual";
            d["addPrepareNew"] = "Preparar un inicio de sesión nuevo";
            d["saveFirstPrompt"] = "¿Guardar primero la cuenta actual?";
            d["accountNamePrompt"] = "Nombre de la cuenta";
            d["deleteConfirm"] = "¿Eliminar {0}? No se puede deshacer.";
            d["editNameTip"] = "Cambiar nombre";
            d["deleteTip"] = "Eliminar";
            d["emptyState"] = "Todavía no hay cuentas guardadas";
            d["statusSwitching"] = "Cambiando a {0}…";
            d["statusOpening"] = "Abriendo Spotify…";
            d["statusSaved"] = "Cuenta guardada: {0}";
            d["statusDeleted"] = "Cuenta eliminada: {0}";
            d["statusRenamed"] = "Nombre cambiado: {0}";
            d["statusPrepared"] = "Listo: inicia sesión con la cuenta nueva en Spotify";
            d["errSpotifyNotInstalled"] = "No encontré Spotify instalado en este PC.";
            d["errSpotifyWouldNotClose"] = "Spotify no se quiso cerrar. Ciérralo y vuelve a intentar.";
            d["errSwitchFailed"] = "No se pudo cambiar de cuenta.";
            d["errStoreVersion"] = "La versión de Spotify de la Microsoft Store no es compatible. Instala la versión de escritorio desde spotify.com.";
            d["errorWithDetail"] = "No se pudo completar: {0}";
            d["toggle"] = "Mostrar al abrir Spotify";
            d["diagnostics"] = "Diagnóstico";
            d["diagnosticsTitle"] = "Diagnóstico de Who's Listening?";
            d["copy"] = "Copiar";
            d["ok"] = "Aceptar";
            d["cancel"] = "Cancelar";
            d["yes"] = "Sí";
            d["no"] = "No";
            d["delete"] = "Eliminar";
            d["close"] = "Cerrar";
            d["minimize"] = "Minimizar";
            d["trayChooseAccount"] = "Elegir cuenta";
            d["trayToggle"] = "Mostrar al abrir Spotify";
            d["trayExit"] = "Salir";
            d["trayTooltip"] = "Who's Listening? - cambiar de cuenta de Spotify";
            d["installPrompt"] = "¿Instalar Who's Listening? en este PC?";
            d["installed"] = "Who's Listening? quedó instalado y se abrirá al iniciar Windows.";
            d["installedNoStartup"] = "Who's Listening? quedó instalado. Ábrelo desde el menú Inicio cuando quieras cambiar de cuenta.";
            d["uninstallConfirm"] = "¿Desinstalar Who's Listening?";
            d["uninstallDeleteAccounts"] = "¿Borrar también las cuentas guardadas?";
            d["uninstalled"] = "Who's Listening? se desinstaló.";
            d["importPrompt"] = "Cuentas guardadas por un selector de cuentas anterior: {0}. ¿Importarlas?";
            d["importResult"] = "Cuentas importadas: {0}.";
            d["subtitle"] = "Elige una cuenta para abrir Spotify";
            d["profile"] = "Perfil";
            d["addAccountSub"] = "Nueva o actual";
            d["errNoCurrentAccount"] = "No hay una cuenta con sesión iniciada en Spotify.";
            d["appName"] = "Who's Listening?";
            d["spotify"] = "Spotify";
            d["errNoCredentials"] = "Spotify no tiene la sesión guardada para esta cuenta. Inicia sesión marcando «Recordarme» y vuelve a intentar.";
            return d;
        }

        private static Dictionary<string, string> BuildEn()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d["title"] = "Who's listening?";
            d["inUse"] = "In use";
            d["addAccount"] = "Add account";
            d["addSaveCurrent"] = "Save the current account";
            d["addPrepareNew"] = "Prepare a new sign-in";
            d["saveFirstPrompt"] = "Save the current account first?";
            d["accountNamePrompt"] = "Account name";
            d["deleteConfirm"] = "Delete {0}? This can't be undone.";
            d["editNameTip"] = "Rename";
            d["deleteTip"] = "Delete";
            d["emptyState"] = "No saved accounts yet";
            d["statusSwitching"] = "Switching to {0}…";
            d["statusOpening"] = "Opening Spotify…";
            d["statusSaved"] = "Account saved: {0}";
            d["statusDeleted"] = "Account deleted: {0}";
            d["statusRenamed"] = "Renamed: {0}";
            d["statusPrepared"] = "Ready: sign in with the new account in Spotify";
            d["errSpotifyNotInstalled"] = "I couldn't find Spotify installed on this PC.";
            d["errSpotifyWouldNotClose"] = "Spotify wouldn't close. Close it and try again.";
            d["errSwitchFailed"] = "Couldn't switch accounts.";
            d["errStoreVersion"] = "The Microsoft Store version of Spotify isn't supported. Install the desktop version from spotify.com.";
            d["errorWithDetail"] = "Couldn't complete: {0}";
            d["toggle"] = "Show when Spotify opens";
            d["diagnostics"] = "Diagnostics";
            d["diagnosticsTitle"] = "Who's Listening? diagnostics";
            d["copy"] = "Copy";
            d["ok"] = "OK";
            d["cancel"] = "Cancel";
            d["yes"] = "Yes";
            d["no"] = "No";
            d["delete"] = "Delete";
            d["close"] = "Close";
            d["minimize"] = "Minimize";
            d["trayChooseAccount"] = "Choose account";
            d["trayToggle"] = "Show when Spotify opens";
            d["trayExit"] = "Exit";
            d["trayTooltip"] = "Who's Listening? - switch Spotify account";
            d["installPrompt"] = "Install Who's Listening? on this PC?";
            d["installed"] = "Who's Listening? is installed and will start with Windows.";
            d["installedNoStartup"] = "Who's Listening? is installed. Open it from the Start menu whenever you want to switch accounts.";
            d["uninstallConfirm"] = "Uninstall Who's Listening?";
            d["uninstallDeleteAccounts"] = "Also delete the saved accounts?";
            d["uninstalled"] = "Who's Listening? was uninstalled.";
            d["importPrompt"] = "Accounts saved by a previous account switcher: {0}. Import them?";
            d["importResult"] = "Accounts imported: {0}.";
            d["subtitle"] = "Choose an account to open Spotify";
            d["profile"] = "Profile";
            d["addAccountSub"] = "New or current";
            d["errNoCurrentAccount"] = "No account is signed in to Spotify.";
            d["appName"] = "Who's Listening?";
            d["spotify"] = "Spotify";
            d["errNoCredentials"] = "Spotify has no saved session for this account. Sign in with “Remember me” checked and try again.";
            return d;
        }

        private static Dictionary<string, string> BuildPt()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d["title"] = "Quem está ouvindo?";
            d["inUse"] = "Em uso";
            d["addAccount"] = "Adicionar conta";
            d["addSaveCurrent"] = "Salvar a conta atual";
            d["addPrepareNew"] = "Preparar um novo login";
            d["saveFirstPrompt"] = "Salvar a conta atual primeiro?";
            d["accountNamePrompt"] = "Nome da conta";
            d["deleteConfirm"] = "Excluir {0}? Isso não pode ser desfeito.";
            d["editNameTip"] = "Renomear";
            d["deleteTip"] = "Excluir";
            d["emptyState"] = "Ainda não há contas salvas";
            d["statusSwitching"] = "Mudando para {0}…";
            d["statusOpening"] = "Abrindo o Spotify…";
            d["statusSaved"] = "Conta salva: {0}";
            d["statusDeleted"] = "Conta excluída: {0}";
            d["statusRenamed"] = "Nome alterado: {0}";
            d["statusPrepared"] = "Pronto: entre com a nova conta no Spotify";
            d["errSpotifyNotInstalled"] = "Não encontrei o Spotify instalado neste PC.";
            d["errSpotifyWouldNotClose"] = "O Spotify não quis fechar. Feche-o e tente de novo.";
            d["errSwitchFailed"] = "Não foi possível trocar de conta.";
            d["errStoreVersion"] = "A versão do Spotify da Microsoft Store não é compatível. Instale a versão para desktop em spotify.com.";
            d["errorWithDetail"] = "Não foi possível concluir: {0}";
            d["toggle"] = "Mostrar ao abrir o Spotify";
            d["diagnostics"] = "Diagnóstico";
            d["diagnosticsTitle"] = "Diagnóstico do Who's Listening?";
            d["copy"] = "Copiar";
            d["ok"] = "OK";
            d["cancel"] = "Cancelar";
            d["yes"] = "Sim";
            d["no"] = "Não";
            d["delete"] = "Excluir";
            d["close"] = "Fechar";
            d["minimize"] = "Minimizar";
            d["trayChooseAccount"] = "Escolher conta";
            d["trayToggle"] = "Mostrar ao abrir o Spotify";
            d["trayExit"] = "Sair";
            d["trayTooltip"] = "Who's Listening? - trocar de conta do Spotify";
            d["installPrompt"] = "Instalar o Who's Listening? neste PC?";
            d["installed"] = "O Who's Listening? foi instalado e abrirá com o Windows.";
            d["installedNoStartup"] = "O Who's Listening? foi instalado. Abra-o pelo menu Iniciar quando quiser trocar de conta.";
            d["uninstallConfirm"] = "Desinstalar o Who's Listening?";
            d["uninstallDeleteAccounts"] = "Excluir também as contas salvas?";
            d["uninstalled"] = "O Who's Listening? foi desinstalado.";
            d["importPrompt"] = "Contas salvas por um seletor de contas anterior: {0}. Importá-las?";
            d["importResult"] = "Contas importadas: {0}.";
            d["subtitle"] = "Escolha uma conta para abrir o Spotify";
            d["profile"] = "Perfil";
            d["addAccountSub"] = "Nova ou atual";
            d["errNoCurrentAccount"] = "Não há uma conta conectada no Spotify.";
            d["appName"] = "Who's Listening?";
            d["spotify"] = "Spotify";
            d["errNoCredentials"] = "O Spotify não tem a sessão salva para esta conta. Entre marcando “Lembrar de mim” e tente de novo.";
            return d;
        }

        private static Dictionary<string, string> BuildFr()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d["title"] = "Qui écoute ?";
            d["inUse"] = "Actif";
            d["addAccount"] = "Ajouter un compte";
            d["addSaveCurrent"] = "Enregistrer le compte actuel";
            d["addPrepareNew"] = "Préparer une nouvelle connexion";
            d["saveFirstPrompt"] = "Enregistrer d'abord le compte actuel ?";
            d["accountNamePrompt"] = "Nom du compte";
            d["deleteConfirm"] = "Supprimer {0} ? Cette action est irréversible.";
            d["editNameTip"] = "Renommer";
            d["deleteTip"] = "Supprimer";
            d["emptyState"] = "Aucun compte enregistré pour le moment";
            d["statusSwitching"] = "Passage à {0}…";
            d["statusOpening"] = "Ouverture de Spotify…";
            d["statusSaved"] = "Compte enregistré : {0}";
            d["statusDeleted"] = "Compte supprimé : {0}";
            d["statusRenamed"] = "Nom modifié : {0}";
            d["statusPrepared"] = "Prêt : connectez-vous avec le nouveau compte dans Spotify";
            d["errSpotifyNotInstalled"] = "Je n'ai pas trouvé Spotify installé sur ce PC.";
            d["errSpotifyWouldNotClose"] = "Spotify ne veut pas se fermer. Fermez-le et réessayez.";
            d["errSwitchFailed"] = "Impossible de changer de compte.";
            d["errStoreVersion"] = "La version Microsoft Store de Spotify n'est pas prise en charge. Installez la version de bureau depuis spotify.com.";
            d["errorWithDetail"] = "Impossible de terminer : {0}";
            d["toggle"] = "Afficher à l'ouverture de Spotify";
            d["diagnostics"] = "Diagnostic";
            d["diagnosticsTitle"] = "Diagnostic de Who's Listening?";
            d["copy"] = "Copier";
            d["ok"] = "OK";
            d["cancel"] = "Annuler";
            d["yes"] = "Oui";
            d["no"] = "Non";
            d["delete"] = "Supprimer";
            d["close"] = "Fermer";
            d["minimize"] = "Réduire";
            d["trayChooseAccount"] = "Choisir un compte";
            d["trayToggle"] = "Afficher à l'ouverture de Spotify";
            d["trayExit"] = "Quitter";
            d["trayTooltip"] = "Who's Listening? - changer de compte Spotify";
            d["installPrompt"] = "Installer Who's Listening? sur ce PC ?";
            d["installed"] = "Who's Listening? est installé et démarrera avec Windows.";
            d["installedNoStartup"] = "Who's Listening? est installé. Ouvrez-le depuis le menu Démarrer quand vous voulez changer de compte.";
            d["uninstallConfirm"] = "Désinstaller « Who's Listening? » ?";
            d["uninstallDeleteAccounts"] = "Supprimer aussi les comptes enregistrés ?";
            d["uninstalled"] = "Who's Listening? a été désinstallé.";
            d["importPrompt"] = "Comptes enregistrés par un ancien sélecteur de comptes : {0}. Les importer ?";
            d["importResult"] = "Comptes importés : {0}.";
            d["subtitle"] = "Choisissez un compte pour ouvrir Spotify";
            d["profile"] = "Profil";
            d["addAccountSub"] = "Nouveau ou actuel";
            d["errNoCurrentAccount"] = "Aucun compte n'est connecté dans Spotify.";
            d["appName"] = "Who's Listening?";
            d["spotify"] = "Spotify";
            d["errNoCredentials"] = "Spotify n'a pas de session enregistrée pour ce compte. Connectez-vous en cochant « Se souvenir de moi » et réessayez.";
            return d;
        }

        private static Dictionary<string, string> BuildDe()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d["title"] = "Wer hört zu?";
            d["inUse"] = "Aktiv";
            d["addAccount"] = "Konto hinzufügen";
            d["addSaveCurrent"] = "Aktuelles Konto speichern";
            d["addPrepareNew"] = "Neue Anmeldung vorbereiten";
            d["saveFirstPrompt"] = "Aktuelles Konto zuerst speichern?";
            d["accountNamePrompt"] = "Kontoname";
            d["deleteConfirm"] = "{0} löschen? Das kann nicht rückgängig gemacht werden.";
            d["editNameTip"] = "Umbenennen";
            d["deleteTip"] = "Löschen";
            d["emptyState"] = "Noch keine Konten gespeichert";
            d["statusSwitching"] = "Wechsle zu {0}…";
            d["statusOpening"] = "Spotify wird geöffnet…";
            d["statusSaved"] = "Konto gespeichert: {0}";
            d["statusDeleted"] = "Konto gelöscht: {0}";
            d["statusRenamed"] = "Umbenannt: {0}";
            d["statusPrepared"] = "Bereit: Melde dich in Spotify mit dem neuen Konto an";
            d["errSpotifyNotInstalled"] = "Spotify ist auf diesem PC nicht installiert.";
            d["errSpotifyWouldNotClose"] = "Spotify ließ sich nicht schließen. Schließe es und versuche es erneut.";
            d["errSwitchFailed"] = "Das Konto konnte nicht gewechselt werden.";
            d["errStoreVersion"] = "Die Microsoft-Store-Version von Spotify wird nicht unterstützt. Installiere die Desktop-Version von spotify.com.";
            d["errorWithDetail"] = "Vorgang nicht möglich: {0}";
            d["toggle"] = "Beim Öffnen von Spotify anzeigen";
            d["diagnostics"] = "Diagnose";
            d["diagnosticsTitle"] = "Who's Listening? Diagnose";
            d["copy"] = "Kopieren";
            d["ok"] = "OK";
            d["cancel"] = "Abbrechen";
            d["yes"] = "Ja";
            d["no"] = "Nein";
            d["delete"] = "Löschen";
            d["close"] = "Schließen";
            d["minimize"] = "Minimieren";
            d["trayChooseAccount"] = "Konto wählen";
            d["trayToggle"] = "Beim Öffnen von Spotify anzeigen";
            d["trayExit"] = "Beenden";
            d["trayTooltip"] = "Who's Listening? - Spotify-Konto wechseln";
            d["installPrompt"] = "Who's Listening? auf diesem PC installieren?";
            d["installed"] = "Who's Listening? ist installiert und startet mit Windows.";
            d["installedNoStartup"] = "Who's Listening? ist installiert. Öffne es über das Startmenü, wenn du das Konto wechseln möchtest.";
            d["uninstallConfirm"] = "Who's Listening? deinstallieren?";
            d["uninstallDeleteAccounts"] = "Auch die gespeicherten Konten löschen?";
            d["uninstalled"] = "Who's Listening? wurde deinstalliert.";
            d["importPrompt"] = "Von einem früheren Kontowechsler gespeicherte Konten: {0}. Importieren?";
            d["importResult"] = "Importierte Konten: {0}.";
            d["subtitle"] = "Wähle ein Konto, um Spotify zu öffnen";
            d["profile"] = "Profil";
            d["addAccountSub"] = "Neu oder aktuell";
            d["errNoCurrentAccount"] = "In Spotify ist kein Konto angemeldet.";
            d["appName"] = "Who's Listening?";
            d["spotify"] = "Spotify";
            d["errNoCredentials"] = "Spotify hat für dieses Konto keine gespeicherte Sitzung. Melde dich mit „Angemeldet bleiben“ an und versuche es erneut.";
            return d;
        }
    }
}

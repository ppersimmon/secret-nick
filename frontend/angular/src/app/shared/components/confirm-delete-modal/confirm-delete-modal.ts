import { Component, input, output, HostBinding, computed } from '@angular/core';
import { CommonModalTemplate } from '../modal/common-modal-template/common-modal-template';
import { ButtonText, PictureName } from '../../../app.enum';

@Component({
  selector: 'app-confirm-delete-modal',
  standalone: true,
  imports: [CommonModalTemplate],
  templateUrl: './confirm-delete-modal.html',
  styleUrl: './confirm-delete-modal.scss',
})
export class ConfirmDeleteModalComponent {
  @HostBinding('class.confirm-delete-modal') readonly hostClass = true;

  readonly fullName = input.required<string>();

  readonly buttonAction = output<void>();
  readonly cancelButtonAction = output<void>();
  readonly closeModal = output<void>();

  public readonly picture = PictureName.Gift;
  public readonly title = computed(() => `Delete ${this.fullName()}?`);
  public readonly okText = ButtonText.Complete;
  public readonly cancelText = ButtonText.Cancel;

  public onOk(): void {
    this.buttonAction.emit();
  }

  public onCancel(): void {
    this.cancelButtonAction.emit();
  }

  public onClose(): void {
    this.closeModal.emit();
  }
}
